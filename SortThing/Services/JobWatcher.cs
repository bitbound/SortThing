using Microsoft.Extensions.Logging;
using SortThing.Models;
using SortThing.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SortThing.Services
{
    public interface IJobWatcher
    {
        Task CancelWatchers();
        Task WatchJobs(string configPath, bool dryRun, CancellationToken cancelToken);
    }

    public class JobWatcher : IJobWatcher
    {
        private readonly ConcurrentDictionary<object, SemaphoreSlim> _jobRunLocks = new();
        private readonly IJobRunner _jobRunner;
        private readonly ILogger<JobWatcher> _logger;
        private readonly IReportWriter _reportWriter;
        private readonly List<FileSystemWatcher> _watchers = new();
        private readonly SemaphoreSlim _watchersLock = new(1, 1);

        public JobWatcher(IJobRunner jobRunner, IReportWriter reportWriter, ILogger<JobWatcher> logger)
        {
            _jobRunner = jobRunner;
            _reportWriter = reportWriter;
            _logger = logger;
        }

        public Task CancelWatchers()
        {
            foreach (var watcher in _watchers)
            {
                try
                {
                    watcher.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while disposing of watcher.");
                }
                finally
                {
                    _watchers.Remove(watcher);
                }
            }
            return Task.CompletedTask;
        }

        public async Task WatchJobs(string configPath, bool dryRun, CancellationToken cancelToken)
        {
            try
            {
                await _watchersLock.WaitAsync(cancelToken);

                if (string.IsNullOrWhiteSpace(configPath))
                {
                    throw new ArgumentNullException(nameof(configPath));
                }

                var configString = await File.ReadAllTextAsync(configPath, cancelToken);
                var config = JsonSerializer.Deserialize<SortConfig>(configString);

                if (config is null)
                {
                    throw new SerializationException("Config file could not be deserialized.");
                }

                await CancelWatchers();

                foreach (var job in config.Jobs)
                {
                    var key = Guid.NewGuid();
                    var watcher = new FileSystemWatcher(job.SourceDirectory)
                    {
                        IncludeSubdirectories = true
                    };

                    foreach (var ext in job.IncludeExtensions)
                    {
                        watcher.Filters.Add($"*.{ext.Replace(".", "")}");
                    }

                    _watchers.Add(watcher);

                    watcher.Created += (sender, ev) =>
                    {
                        _ = RunJob(key, job, dryRun, cancelToken);
                    };

                    watcher.EnableRaisingEvents = true;
                }
            }
            finally
            {
                _watchersLock.Release();
            }
        }

        private async Task RunJob(Guid jobKey, SortJob job, bool dryRun, CancellationToken cancelToken)
        {
            var jobRunLock = _jobRunLocks.GetOrAdd(jobKey, key =>
            {
                return new SemaphoreSlim(1, 1);
            });

            if (!await jobRunLock.WaitAsync(0, cancelToken))
            {
                return;
            }

            Debouncer.Debounce(jobKey, TimeSpan.FromSeconds(5), async () =>
            {
                try
                {
                    var report = await _jobRunner.RunJob(job, dryRun, cancelToken);
                    await _reportWriter.WriteReport(report);
                }
                finally
                {
                    _jobRunLocks.TryRemove(jobKey, out _);
                }
            });
         
        }
    }
}
