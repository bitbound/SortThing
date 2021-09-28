using Microsoft.Extensions.DependencyInjection;
using SortThing.Services;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace SortThing
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Sort your photos into folders based on metadata.");

            var opt1 = new Option<string>(
                new[] { "--config-path", "-c" },
                "The full path to the SortThing configuration file.  See the readme for an example: https://github.com/lucent-sea/SortThing");
            opt1.AddValidator(option =>
            {
                if (File.Exists(option.GetValueOrDefault()?.ToString()))
                {
                    return null;
                }

                return "Config file could not be found at the given path.";
            });
            rootCommand.AddOption(opt1);

            var opt2 = new Option<bool>(
                new[] { "--once", "-o" },
                () => false,
                "If true, will run sort jobs immediately, then exit.  If false, will run jobs, then block and monitor for changes in each job's source folder.");
            rootCommand.AddOption(opt2);

            var opt3 = new Option<bool>(
                new[] { "--dry-run", "-d" },
                () => false,
                "If true, no file operations will actually be executed.");
            rootCommand.AddOption(opt3);

            rootCommand.Handler = CommandHandler.Create((string configPath, bool once, bool dryRun) =>
            {
                return Run(configPath, once, dryRun, CancellationToken.None);
            });

            return await rootCommand.InvokeAsync(args);
        }

        public static async Task Run(string configPath, bool once, bool dryRun, CancellationToken appExit)
        {
            try
            {
                ServiceContainer.Build();

                if (OperatingSystem.IsWindows() &&
                    !Environment.UserInteractive &&
                    Process.GetCurrentProcess().SessionId == 0)
                {
                    ServiceBase.Run(new WindowsService());
                }

                if (string.IsNullOrWhiteSpace(configPath))
                {
                    var logger = ServiceContainer.Instance.GetRequiredService<ILogger>();

                    await logger.Write("Config path not specified.  Looking in application directory.");

                    var appDir = Path.GetDirectoryName(typeof(Program).Assembly.Location);

                    var matches = Directory.GetFiles(appDir, "*config*.json", new EnumerationOptions()
                    {
                        MatchCasing = MatchCasing.CaseInsensitive,
                        RecurseSubdirectories = true
                    });

                    if (matches.Any())
                    {
                        configPath = matches.First();
                        await logger.Write($"Found config file: {configPath}.");
                    }
                    else
                    {
                        await logger.Write("No config files found.  Exiting.");
                        return;
                    }
                }

                if (once)
                {
                    await ServiceContainer.Instance.GetRequiredService<IJobRunner>().RunJobs(configPath, dryRun);
                }
                else
                {
                    await ServiceContainer.Instance.GetRequiredService<IJobWatcher>().WatchJobs(configPath, dryRun);
                    await Task.Delay(-1, appExit);
                }
            }
            catch (Exception ex)
            {
                await ServiceContainer.Instance.GetRequiredService<ILogger>().Write(ex);
            }
        }
    }
}
