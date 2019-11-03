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
    public abstract class BaseEpisodeResult : BaseResult
    {
        public int? AbsoluteEpisodeNumber { get; set; }
        public DateTime? AirDateUtc { get; set; }
        [JsonProperty("id")]
        public long EpisodeId { get; set; }
        public int EpisodeNumber { get; set; }
        public bool HasFile { get; set; }        
        public bool Monitored { get; set; }
        [JsonProperty("title")]
        public string Name { get; set; }
        public int SeriesId { get; set; }
        public bool UnverifiedSceneNumbering { get; set; }
    }
}
