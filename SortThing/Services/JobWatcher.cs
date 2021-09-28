using SortThing.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SortThing.Services
{
    public interface IJobWatcher
    {
        Task WatchJobs(string configPath, bool dryRun);
    }

    public class JobWatcher : IJobWatcher
    {
        private readonly static List<FileSystemWatcher> _watchers = new();
        private readonly static SemaphoreSlim _watchersLock = new(1, 1);

        private readonly IJobRunner _jobRunner;
        private readonly IFileLogger _logger;

        public JobWatcher(IJobRunner jobRunner, IFileLogger logger)
        {
            _jobRunner = jobRunner;
            _logger = logger;
        }

        public async Task WatchJobs(string configPath, bool dryRun)
        {
            try
            {
                await _watchersLock.WaitAsync();

                if (string.IsNullOrWhiteSpace(configPath))
                {
                    throw new ArgumentNullException(nameof(configPath));
                }

                var configString = await File.ReadAllTextAsync(configPath);
                var config = JsonSerializer.Deserialize<SortConfig>(configString);

                foreach (var watcher in _watchers)
                {
                    try
                    {
                        watcher.Dispose();
                    }
                    catch (Exception ex)
                    {
                        await _logger.Write(ex);
                    }
                    finally
                    {
                        _watchers.Remove(watcher);
                    }
                }

                foreach (var job in config.Jobs)
                {
                    var watcher = new FileSystemWatcher(job.SourceDirectory);

                    foreach (var ext in job.IncludeExtensions)
                    {
                        watcher.Filters.Add($"*.{ext.Replace(".", "")}");
                    }

                    _watchers.Add(watcher);

                    watcher.Created += (object sender, FileSystemEventArgs e) =>
                    {
                        _ = _jobRunner.RunJob(job, dryRun);
                    };

                    watcher.EnableRaisingEvents = true;
                }
            }
            finally
            {
                _watchersLock.Release();
            }
        }
    }
}
