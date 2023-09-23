using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Json.Attributes;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Shell.Models.Series
{
    [JsonIncludePrivateFields(nameof(_rootFolder), nameof(_path))]
    public sealed class SonarrAddSeries : IJsonOnSerializing
    {
        [JsonPropertyName("rootFolderPath")]
        private string? _rootFolder;

        [JsonPropertyName("path")]
        private string? _path;

        public SeriesImage[] Images { get; set; } = Array.Empty<SeriesImage>();

        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public bool IsFullPath { get; set; }

        [JsonPropertyName("monitored")]
        public bool IsMonitored { get; set; } = true;

        [JsonPropertyName("title")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("addOptions")]
        public SeriesAddOptions Options { get; } = new();

        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public string Path { get; set; } = string.Empty;

        public int QualityProfileId { get; set; }

        public PSObject[] Seasons { get; set; } = Array.Empty<PSObject>();

        public string SeriesType { get; set; } = string.Empty;

        public HashSet<int> Tags { get; set; } = new();

        public string TitleSlug { get; set; } = string.Empty;

        [JsonPropertyName("tvdbId")]
        public long TVDbId { get; set; }

        [JsonPropertyName("tvMazeId")]
        public long? TVMazeId { get; set; }

        [JsonPropertyName("tvRageId")]
        public long? TVRageId { get; set; }

        [JsonPropertyName("seasonFolder")]
        public bool UseSeasonFolders { get; set; }

        public void OnSerializing()
        {
            if (this.IsFullPath)
            {
                _path = this.Path;
            }
            else
            {
                _rootFolder = this.Path;
            }
        }
    }
}
