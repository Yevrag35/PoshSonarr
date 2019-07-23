using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Results
{
    public abstract class BaseEpisodeResult : BaseResult
    {
        public int? AbsoluteEpisodeNumber { get; set; }
        public DateTime? AirDateUtc { get; set; }
        [JsonProperty("Id")]
        public long EpisodeId { get; set; }
        public int EpisodeNumber { get; set; }
        public bool HasFile { get; set; }        
        public bool Monitored { get; set; }
        public int SeriesId { get; set; }
        public string Title { get; set; }
        public bool UnverifiedSceneNumbering { get; set; }
    }
}
