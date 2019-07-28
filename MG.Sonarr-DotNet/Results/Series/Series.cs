using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MG.Sonarr.Results
{
    [Serializable]
    public class SeriesResult : BaseResult
    {

        public DateTime? Added { get; set; }
        public string AirTime { get; set; }
        public AlternateTitle[] AlternateTitles { get; set; }
        public string Certification { get; set; }
        public string CleanTitle { get; set; }
        public DateTime? FirstAired { get; set; }
        public string[] Genres { get; set; }
        [JsonProperty("id")]
        public long? SeriesId { get; set; }
        public SeriesImage[] Images { get; set; }
        public string IMDBId { get; set; }
        public bool Monitored { get; set; }
        [JsonProperty("title")]
        public string Name { get; set; }
        public string Network { get; set; }
        public string Overview { get; set; }
        public string Path { get; set; }
        public int ProfileId { get; set; }
        public int QualityProfileId { get; set; }
        public Ratings Ratings { get; set; }
        public string RemotePoster { get; set; }
        public long? Runtime { get; set; }
        public int? SeasonCount { get; set; }
        public bool SeasonFolder { get; set; }
        public SeasonCollection Seasons { get; set; }
        public string SeriesType { get; set; }
        public string SortTitle { get; set; }
        public string Status { get; set; }
        public object[] Tags { get; set; }
        public string TitleSlug { get; set; }
        public long TVDBId { get; set; }
        public long? TVMazeId { get; set; }
        public long? TVRageId { get; set; }
        public bool UseSceneNumbering { get; set; }
        public int Year { get; set; }
    }
}
