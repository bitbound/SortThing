using Microsoft.Extensions.Logging;
using SortThing.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SortThing.Services
{
    public interface IJobRunner
    {
        Task RunJob(SortJob job, bool dryRun);

        Task RunJobs(string configPath, bool dryRun);
    }

    public class JobRunner : IJobRunner
    {
        private static readonly SemaphoreSlim _runLock = new(1, 1);
        private readonly EnumerationOptions _enumOptions = new EnumerationOptions()
        {
            AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Hidden | FileAttributes.System,
            RecurseSubdirectories = true
        };

        private readonly ILogger<JobRunner> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IMetadataReader _metaDataReader;
        private readonly IPathTransformer _pathTransformer;


        public JobRunner(IFileSystem fileSystem, IMetadataReader metaDataReader, IPathTransformer pathTransformer, ILogger<JobRunner> logger)
        {
            // TODO: Implement and use IFileSystem.
            _fileSystem = fileSystem;
            _metaDataReader = metaDataReader;
            _pathTransformer = pathTransformer;
            _logger = logger;
        }

        public async Task RunJob(SortJob job, bool dryRun)
        {
            try
            {
                await _runLock.WaitAsync();

                _logger.LogInformation($"Starting job run: {JsonSerializer.Serialize(job)}");

                foreach (var extension in job.IncludeExtensions)
                {
                    var files = Directory.GetFiles(job.SourceDirectory, $"*.{extension.Replace(".","")}", _enumOptions)
                        .Where(file => !job.ExcludeExtensions.Any(ext => ext.Equals(Path.GetExtension(file)[1..], StringComparison.OrdinalIgnoreCase)));
                    
                    foreach (var file in files)
                    {
                        try
                        {
                            var result = await _metaDataReader.TryGetExifData(file);

                            string destinationFile;

                            if (result.IsSuccess)
                            {
                                destinationFile = _pathTransformer.TransformPath(
                                    file,
                                    job.DestinationFile,
                                    result.Value.DateTaken,
                                    result.Value.CameraModel);
                            }
                            else
                            {
                                var fileCreated = File.GetCreationTime(file);
                                destinationFile = _pathTransformer.TransformPath(file, job.DestinationFile, fileCreated);
                            }

                            if (dryRun)
                            {
                                _logger.LogInformation($"Dry run. Skipping file operation.  Source: {file}.  Destination: {destinationFile}.");
                                continue;
                            }

                            if (File.Exists(destinationFile) &&
                                !job.OverwriteDestination &&
                                !job.CreateNewIfExists)
                            {
                                _logger.LogInformation($"Destination file exists.  Skipping.  Destination file: {destinationFile}");
                                continue;
                            }

                            if (job.CreateNewIfExists)
                            {
                                destinationFile = _pathTransformer.GetUniqueFilePath(destinationFile);
                            }

                            _logger.LogInformation($"Starting file operation: {job.Operation}.  Source: {file}.  Destination: {destinationFile}.");

                            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

                            switch (job.Operation)
                            {
                                case Enums.SortOperation.Move:
                                    File.Move(file, destinationFile, true);
                                    break;
                                case Enums.SortOperation.Copy:
                                    File.Copy(file, destinationFile, true);
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error while running job.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while running job.");
            }
            finally
            {
                _runLock.Release();
            }
        }

        public async Task RunJobs(string configPath, bool dryRun)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                throw new ArgumentNullException(nameof(configPath));
            }

            var configString = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<SortConfig>(configString);

            foreach (var job in config.Jobs)
            {
                await RunJob(job, dryRun);
            }
        }
    }
}
