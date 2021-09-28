using ExifLib;
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
        Result<ExifData> TryGetExifData(string filePath);
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


        public Result<ExifData> TryGetExifData(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return Result.Fail<ExifData>("File could not be found.");
                }

                using var reader = new ExifReader(filePath);

                if (!reader.GetTagValue<DateTime>(ExifTags.DateTimeOriginal, out var dateTaken) &&
                    !reader.GetTagValue(ExifTags.DateTimeDigitized, out dateTaken))
                {
                    return Result.Fail<ExifData>("DateTime is missing from metadata.");
                }

                reader.GetTagValue<string>(ExifTags.Model, out var camera);

                return Result.Ok(new ExifData()
                {
                    DateTaken = dateTaken,
                    CameraModel = camera?.Trim()
                });
            }
            catch
            {
                return Result.Fail<ExifData>("Error while reading metadata.");
            }
        }
    }
}
