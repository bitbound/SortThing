using Microsoft.Extensions.Hosting;
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
        private readonly IFileLogger _logger;

        public SortBackgroundService(IJobRunner jobRunner, IJobWatcher jobWatcher, IFileLogger logger)
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
                    await _logger.Write("Config path not specified.  Looking for config.json in application directory.");

                    var appDir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                    var configPath = Path.Combine(appDir, "config.json");

                    if (File.Exists(configPath))
                    {
                        await _logger.Write($"Found config file: {configPath}.");
                    }
                    else
                    {
                        await _logger.Write("No config file was found.  Exiting.");
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
                await _logger.Write(ex);
            }
        }
    }
}
