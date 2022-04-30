using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SortThing.Services;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SortThing
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Sort your photos into folders based on metadata.");

            var configOption = new Option<string>(
                new[] { "--config-path", "-c" },
                "The full path to the SortThing configuration file.  See the readme for an example: https://github.com/lucent-sea/SortThing");
            configOption.AddValidator(option =>
            {
                if (!File.Exists(option.GetValueOrDefault()?.ToString()))
                {
                    option.ErrorMessage = "Config file could not be found at the given path.";
                }
            });
            rootCommand.AddOption(configOption);

            var jobOption = new Option<string>(
                new[] { "--job-name", "-j" },
                () => string.Empty,
                "If specified, will only run the named job from the config, then exit.");
                        rootCommand.AddOption(jobOption);

            var watchOption = new Option<bool>(
                new[] { "--watch", "-w" },
                () => false,
                "If false, will run sort jobs immediately, then exit.  If true, will run jobs, then block and monitor for changes in each job's source folder.");
            rootCommand.AddOption(watchOption);

            var dryRunOption = new Option<bool>(
                new[] { "--dry-run", "-d" },
                () => false,
                "If true, no file operations will actually be executed.");
            rootCommand.AddOption(dryRunOption);

            rootCommand.SetHandler(async (string configPath, string jobName, bool watch, bool dryRun) =>
            {
                using var host = Host.CreateDefaultBuilder(args)
                    .UseWindowsService(options =>
                    {
                        options.ServiceName = "SortThing";
                    })
                    .UseConsoleLifetime()
                    .ConfigureServices(services =>
                    {
                        services.AddScoped<IMetadataReader, MetadataReader>();
                        services.AddScoped<IJobRunner, JobRunner>();
                        services.AddSingleton<IJobWatcher, JobWatcher>();
                        services.AddScoped<IPathTransformer, PathTransformer>();
                        services.AddScoped<IFileSystem, FileSystem>();
                        services.AddScoped<IReportWriter, ReportWriter>();
                        services.AddScoped<IConfigService, ConfigService>();
                        services.AddSingleton<ISystemTime, SystemTime>();
                        services.AddSingleton<IGlobalState>(new GlobalState()
                        {
                            ConfigPath = configPath,
                            DryRun = dryRun,
                            JobName = jobName,
                            Watch = watch
                        });
                        services.AddHostedService<SortBackgroundService>();
                    })
                    .ConfigureLogging(builder =>
                    {
                        builder.ClearProviders();
                        builder.AddConsole();
                        builder.AddProvider(new FileLoggerProvider());
                    })
                    .Build();
                
                await host.RunAsync();
            }, configOption, jobOption, watchOption, dryRunOption);

            return await rootCommand.InvokeAsync(args);
        }
    }
}
