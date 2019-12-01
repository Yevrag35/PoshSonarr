using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The base class for all Episode-related response data from Sonarr.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BaseEpisodeResult : BaseResult
    {
        [JsonProperty("absoluteEpisodeNumber")]
        public int? AbsoluteEpisodeNumber { get; internal set; }

        [JsonProperty("airDateUtc")]
        public DateTime? AirDateUtc { get; internal set; }

        [JsonProperty("id")]
        public long EpisodeId { get; internal set; }

        [JsonProperty("episodeNumber")]
        public int EpisodeNumber { get; internal set; }

        // Used for backwards compatibility
        [Obsolete]
        public bool HasFile => this.IsDownloaded;

        [JsonProperty("hasFile")]
        public bool IsDownloaded { get; internal set; }

        [JsonProperty("monitored")]
        public bool IsMonitored { get; set; }

        [JsonIgnore]
        [Obsolete]
        public bool Monitored
        {
            get => this.IsMonitored;
            set
            {
                Console.WriteLine("The property \"Monitored\" is deprecated and will be removed from future releases.  Use \"IsMonitored\" instead.");
                this.IsMonitored = value;
            }
        }

        [JsonProperty("title")]
        public string Name { get; internal set; }

        [JsonProperty("seriesId")]
        public int SeriesId { get; internal set; }

        [JsonProperty("unverifiedSceneNumbering")]
        public bool UnverifiedSceneNumbering { get; internal set; }
    }
}
