# SortThing
Sort photos and files into folders based on metadata.  Pair with [Syncthing](https://github.com/syncthing/syncthing)!

Command Line Info:
```
Description:
  Sort your photos into folders based on metadata.

Usage:
  SortThing [options]

Options:
  -c, --config-path <config-path>  The full path to the SortThing configuration file.  Use -g to generate a sample
                                   config file in the current directory.
  -g, --generate-config            Generates a sample config file in the current directory. [default: False]
  -j, --job-name <job-name>        If specified, will only run the named job from the config, then exit. []
  -w, --watch                      If false, will run sort jobs immediately, then exit.  If true, will run jobs, then
                                   block and monitor for changes in each job's source folder. [default: False]
  -d, --dry-run                    If true, no file operations will actually be executed. [default: False]
  --version                        Show version information
  -?, -h, --help                   Show help and usage information
```