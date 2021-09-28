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
        private readonly ILogger<BackgroundService> _logger;

        public SortBackgroundService(IJobRunner jobRunner, IJobWatcher jobWatcher, ILogger<BackgroundService> logger)
        {
            _jobRunner = jobRunner;
            _jobWatcher = jobWatcher;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Program.ConfigPath))
                {
                    _logger.LogInformation("Config path not specified.  Looking for config.json in application directory.");

                    var appDir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                    var configPath = Path.Combine(appDir, "config.json");

                    if (File.Exists(configPath))
                    {
                        _logger.LogInformation($"Found config file: {configPath}.");
                    }
                    else
                    {
                        _logger.LogWarning("No config file was found.  Exiting.");
                        return;
                    }
                }

                await _jobRunner.RunJobs(Program.ConfigPath, Program.DryRun);

                if (Program.Once)
                {
                    return;
                }

                await _jobWatcher.WatchJobs(Program.ConfigPath, Program.DryRun);
                await Task.Delay(-1, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while starting background service.");
            }
        }
    }
}
