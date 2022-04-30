using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SortThing.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SortThing.Services
{
    public class SortBackgroundService : BackgroundService
    {
        private readonly IJobRunner _jobRunner;
        private readonly IJobWatcher _jobWatcher;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IFileSystem _fileSystem;
        private readonly IGlobalState _globalState;
        private readonly IConfigService _configService;
        private readonly IReportWriter _reportWriter;
        private readonly ILogger<SortBackgroundService> _logger;

        public SortBackgroundService(
            IJobRunner jobRunner, 
            IJobWatcher jobWatcher,
            IHostApplicationLifetime appLifetime, 
            IFileSystem fileSystem,
            IGlobalState globalState,
            IConfigService configService,
            IReportWriter reportWriter,
            ILogger<SortBackgroundService> logger)
        {
            _jobRunner = jobRunner;
            _jobWatcher = jobWatcher;
            _appLifetime = appLifetime;
            _fileSystem = fileSystem;
            _globalState = globalState;
            _configService = configService;
            _reportWriter = reportWriter;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var configPath = _globalState.ConfigPath;
                var cts = new CancellationTokenSource();
                var cancelToken = cts.Token;

                if (string.IsNullOrWhiteSpace(configPath))
                {
                    _logger.LogInformation("Config path not specified.  Looking for config file in application directory.");

                    var result = await _configService.TryFindConfig();
                    if (!result.IsSuccess)
                    {
                        _appLifetime.StopApplication();
                        return;
                    }

                    configPath = result.Value;
                }

                if (!_fileSystem.FileExists(configPath))
                {
                    _logger.LogInformation("Config file not found at {configPath}.", configPath);
                    _appLifetime.StopApplication();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(_globalState.JobName))
                {
                    _appLifetime.StopApplication();
                    var report = await _jobRunner.RunJob(configPath, _globalState.JobName, _globalState.DryRun, stoppingToken);
                    await _reportWriter.WriteReport(report);
                    return;
                }

                var reports = await _jobRunner.RunJobs(configPath, _globalState.DryRun, stoppingToken);
                await _reportWriter.WriteReports(reports);

                if (!_globalState.Watch)
                {
                    _appLifetime.StopApplication();
                    return;
                }

                _logger.LogInformation("Watching for changes...");

                await _jobWatcher.WatchJobs(configPath, _globalState.DryRun, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while starting background service.");
            }
        }
    }
}
