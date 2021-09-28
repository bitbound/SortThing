using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SortThing.Services
{
    public class SortBackgroundService : BackgroundService
    {
        private readonly IJobRunner _jobRunner;
        private readonly IJobWatcher _jobWatcher;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ILogger<SortBackgroundService> _logger;

        public SortBackgroundService(
            IJobRunner jobRunner, 
            IJobWatcher jobWatcher,
            IHostApplicationLifetime appLifetime, 
            ILogger<SortBackgroundService> logger)
        {
            _jobRunner = jobRunner;
            _jobWatcher = jobWatcher;
            _appLifetime = appLifetime;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var configPath = Program.ConfigPath;

                if (string.IsNullOrWhiteSpace(configPath))
                {
                    _logger.LogInformation("Config path not specified.  Looking for config.json in application directory.");

                    var exeDir = Path.GetDirectoryName(Environment.CommandLine.Split(" ").First());
                    configPath = Path.Combine(exeDir, "config.json");

                    if (File.Exists(configPath))
                    {
                        _logger.LogInformation($"Found config file: {configPath}.");
                    }
                    else
                    {
                        _logger.LogWarning($"No config file was found at {configPath}.  Exiting.");
                        _appLifetime.StopApplication();
                        return;
                    }
                }
                
                await _jobRunner.RunJobs(configPath, Program.DryRun);
                
                if (Program.Once)
                {
                    _appLifetime.StopApplication();
                    return;
                }

                await _jobWatcher.WatchJobs(configPath, Program.DryRun);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while starting background service.");
            }
        }
    }
}
