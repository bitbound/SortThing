using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SortThing.Abstractions;
using SortThing.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SortThing.Services
{
    public interface IMetadataReader
    {
        Result<DateTime> ParseExifDateTime(string exifDateTime);
        Task<Result<ExifData>> TryGetExifData(string filePath);
    }

    public class MetadataReader : IMetadataReader
    {
        /// <summary>
        /// Formats an EXIF DateTime to a format that can be parsed in .NET.
        /// </summary>
        /// <param name="exifDateTime"></param>
        /// <returns></returns>
        public Result<DateTime> ParseExifDateTime(string exifDateTime)
        {
            if (string.IsNullOrWhiteSpace(exifDateTime))
            {
                return Result.Fail<DateTime>($"Parameter {nameof(exifDateTime)} cannot be empty.");
            }

            if (exifDateTime.Count(character => character == ':') < 2)
            {
                return Result.Fail<DateTime>($"Parameter {nameof(exifDateTime)} appears to be invalid.");
            }

            var dateArray = exifDateTime
                .Split(" ", StringSplitOptions.RemoveEmptyEntries)
                .Apply(split => split[0] = split[0].Replace(':', '-'));

            if (!DateTime.TryParse(string.Join(' ', dateArray), out var dateTaken))
            {
                return Result.Fail<DateTime>("Unable to parse DateTime metadata value.");
            }

            return Result.Ok(dateTaken);
        }


        public async Task<Result<ExifData>> TryGetExifData(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return Result.Fail<ExifData>("File could not be found.");
                }

                var img = await Image.LoadAsync(filePath);

                var date = img.Metadata?.ExifProfile?.GetValue(ExifTag.DateTimeOriginal) ??
                    img.Metadata?.ExifProfile?.GetValue(ExifTag.DateTimeDigitized);

                if (string.IsNullOrWhiteSpace(date?.Value))
                {
                    return Result.Fail<ExifData>("DateTime is missing from metadata.");
                }

                var parseResult = ParseExifDateTime(date.Value);

                if (!parseResult.IsSuccess)
                {
                    return Result.Fail<ExifData>(parseResult.Error);
                }

                var camera = img.Metadata.ExifProfile.GetValue(ExifTag.Model)?.Value;

                return Result.Ok(new ExifData()
                {
                    DateTaken = parseResult.Value,
                    CameraModel = camera
                });
            }
            catch
            {
                return Result.Fail<ExifData>("Error while reading metadata.");
            }
        }
    }
}
