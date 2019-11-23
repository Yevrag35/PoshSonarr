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
        private int? _aen;
        public int? AbsoluteEpisodeNumber => _aen;

        [JsonProperty("airDateUtc")]
        public DateTime? AirDateUtc { get; set; }

        [JsonProperty("id")]
        private long _epid;
        public long EpisodeId => _epid;

        [JsonProperty("episodeNumber")]
        private int _epn;
        public int EpisodeNumber => _epn;

        // Used for backwards compatibility
        [Obsolete]
        public bool HasFile => this.IsDownloaded;

        [JsonProperty("hasFile")]
        private bool _isd;
        public bool IsDownloaded => _isd;

        [JsonProperty("monitored")]
        public bool Monitored { get; set; }

        [JsonProperty("title")]
        private string _name;
        public string Name => _name;

        [JsonProperty("seriesId")]
        private int _sid;
        public int SeriesId => _sid;

        [JsonProperty("unverifiedSceneNumbering")]
        private bool _unvsn;
        public bool UnverifiedSceneNumbering => _unvsn;
    }
}
