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
        private static CancellationTokenSource _appExitSource = new();
        public static string ConfigPath { get; private set; }
        public static bool DryRun { get; private set; }
        public static bool Once { get; private set; }


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
                ConfigPath = configPath;
                Once = once;
                DryRun = dryRun;

                using var host = Host.CreateDefaultBuilder(args)
                    .UseWindowsService(options =>
                    {
                        options.ServiceName = "SortThing";
                    })
                    .ConfigureServices(services =>
                    {
                        services.AddLogging();
                        services.AddScoped<IMetadataReader, MetadataReader>();
                        services.AddScoped<IJobRunner, JobRunner>();
                        services.AddSingleton<IJobWatcher, JobWatcher>();
                        services.AddScoped<IPathTransformer, PathTransformer>();
                        services.AddScoped<IFileSystem, FileSystem>();
                        services.AddHostedService<SortBackgroundService>();
                    })
                    .ConfigureLogging(builder =>
                    {
                        builder.AddProvider(new FileLoggerProvider());
                    })
                    .Build();
                
                return host.RunAsync();
            });

            return await rootCommand.InvokeAsync(args);
        }
    }
}
