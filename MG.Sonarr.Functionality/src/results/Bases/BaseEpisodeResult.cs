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
        public int? AbsoluteEpisodeNumber { get; set; }

        [JsonProperty("airDateUtc")]
        public DateTime? AirDateUtc { get; set; }

        [JsonProperty("id")]
        public long EpisodeId { get; set; }

        [JsonProperty("episodeNumber")]
        public int EpisodeNumber { get; set; }

        // Used for backwards compatibility
        [Obsolete]
        public bool HasFile => this.IsDownloaded;
        
        [JsonProperty("hasFile")]
        public bool IsDownloaded { get; set; }

        [JsonProperty("monitored")]
        public bool Monitored { get; set; }

        [JsonProperty("title")]
        public string Name { get; set; }

        [JsonProperty("seriesId")]
        public int SeriesId { get; set; }

        [JsonProperty("unverifiedSceneNumbering")]
        public bool UnverifiedSceneNumbering { get; set; }
    }
}
