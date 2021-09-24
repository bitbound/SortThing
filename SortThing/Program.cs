using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SortThing.Enums;
using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;

namespace SortThing
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Sort your photos into folders based on metadata.");

            var opt1 = new Option<string>(
                new[]  { "--config-path", "-c" },
                "The full path to the SortThing configuration file.  See the readme for an example: https://github.com/lucent-sea/SortThing")
            {
                IsRequired = true
            };
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

            rootCommand.Handler = CommandHandler.Create<string, bool, bool>(Init);

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task Init(string configPath, bool once, bool dryRun)
        {
            
        }
    }
}
