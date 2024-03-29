﻿using System;
using System.IO;

namespace SortThing.Services
{
    public interface IPathTransformer
    {
        string TransformPath(string sourcePath, string destinationPath);
        string TransformPath(string sourcePath, string destinationPath, DateTime dateTaken, string camera);
        string GetUniqueFilePath(string destinationFile);
        string TransformPath(string sourcePath, string destinationPath, DateTime fileCreated);
    }

    public class PathTransformer : IPathTransformer
    {
        public const string Camera = "{camera}";
        public const string Day = "{day}";
        public const string Extension = "{extension}";
        public const string Filename = "{filename}";
        public const string Hour = "{hour}";
        public const string Minute = "{minute}";
        public const string Month = "{month}";
        public const string Year = "{year}";

        public string GetUniqueFilePath(string destinationFile)
        {
            var uniquePath = destinationFile;

            for (var i = 0; true; i++)
            {
                if (!File.Exists(uniquePath))
                {
                    break;
                }

                var filename = 
                    Path.GetFileNameWithoutExtension(destinationFile) +
                    $"_{i}" +
                    Path.GetExtension(destinationFile);

                uniquePath = Path.Combine(Path.GetDirectoryName(destinationFile), filename);
            }

            return uniquePath;
        }

        public string TransformPath(string sourceFile, string destinationFile, DateTime dateTaken, string camera)
        {
            if (string.IsNullOrWhiteSpace(sourceFile))
            {
                throw new ArgumentNullException(nameof(sourceFile));
            }

            if (string.IsNullOrWhiteSpace(destinationFile))
            {
                throw new ArgumentNullException(nameof(destinationFile));
            }

            return destinationFile
                .Replace(Year, dateTaken.Year.ToString().PadLeft(4, '0'))
                .Replace(Month, dateTaken.Month.ToString().PadLeft(2, '0'))
                .Replace(Day, dateTaken.Day.ToString().PadLeft(2, '0'))
                .Replace(Hour, dateTaken.Hour.ToString().PadLeft(2, '0'))
                .Replace(Minute, dateTaken.Minute.ToString().PadLeft(2, '0'))
                .Replace(Camera, camera?.Trim())
                .Replace(Filename, Path.GetFileNameWithoutExtension(sourceFile))
                .Replace(Extension, Path.GetExtension(sourceFile)[1..]);
        }

        public string TransformPath(string sourceFile, string destinationFile)
        {
            if (string.IsNullOrWhiteSpace(sourceFile))
            {
                throw new ArgumentNullException(nameof(sourceFile));
            }

            if (string.IsNullOrWhiteSpace(destinationFile))
            {
                throw new ArgumentNullException(nameof(destinationFile));
            }

            return destinationFile
                .Replace(Filename, Path.GetFileNameWithoutExtension(sourceFile))
                .Replace(Extension, Path.GetExtension(sourceFile)[1..]);
        }

        public string TransformPath(string sourcePath, string destinationPath, DateTime fileCreated)
        {
            return TransformPath(sourcePath, destinationPath, fileCreated, string.Empty);
        }
    }
}
