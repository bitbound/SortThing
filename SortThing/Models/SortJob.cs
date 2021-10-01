using SortThing.Enums;
using System.Text.Json.Serialization;

namespace SortThing.Models
{
    public class SortJob
    {
        public bool CreateNewIfExists { get; init; }

        public string DestinationFile { get; init; }

        public string[] ExcludeExtensions { get; init; }

        public string[] IncludeExtensions { get; init; }

        public string Name { get; init; }

        /// <summary>
        /// The operation to perform on the original files.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SortOperation Operation { get; init; }
        public bool OverwriteDestination { get; init; }
        public string SourceDirectory { get; init; }
    }
}
