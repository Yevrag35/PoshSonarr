using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SearchSeries : BaseResult
    {
        #region JSON PROPERTIES
        [JsonProperty("added")]
        public DateTime Added { get; private set; }

        [JsonProperty("airTime")]
        public string AirTime { get; protected private set; }

        [JsonProperty("certification")]
        public string Certification { get; private set; }

        [JsonProperty("cleanTitle")]
        public string CleanTitle { get; private set; }

        [JsonProperty("firstAired")]
        public DateTime FirstAired { get; protected private set; }

        [JsonProperty("genres")]
        public string[] Genres { get; protected private set; }

        [JsonProperty("images")]
        public SeriesImage[] Images { get; protected private set; }

        [JsonProperty("imdbId")]
        public string IMDBId { get; private set; }

        [JsonProperty("monitored")]
        public bool IsMonitored { get; set; }

        [JsonProperty("title")]
        public string Name { get; private set; }

        [JsonProperty("network")]
        public string Network { get; private set; }

        [JsonProperty("overview")]
        public string Overview { get; private set; }

        [JsonIgnore]
        public float Rating => _ratings.Value;

        [JsonProperty("ratings")]
        private Ratings _ratings;

        [JsonIgnore]
        public long RatingVotes => _ratings.Votes;

        //[JsonProperty("ratings")]
        //public Ratings Ratings { get; private set; }

        [JsonProperty("remotePoster")]
        public string PosterUrl { get; private set; }

        [JsonProperty("runtime")]
        public int Runtime { get; protected private set; }

        [JsonProperty("seasonCount")]
        public int SeasonCount { get; private set; }

        [JsonProperty("seasons")]
        public SeasonCollection Season { get; protected private set; }

        [JsonProperty("seriesType")]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public SeriesType SeriesType { get; private set; }

        [JsonProperty("sortTitle")]
        public string SortTitle { get; private set; }

        [JsonProperty("status")]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public SeriesStatusType Status { get; private set; }

        [JsonProperty("titleSlug")]
        public string TitleSlug { get; private set; }

        [JsonProperty("tvdbId")]
        public long TVDBId { get; private set; }

        [JsonProperty("tvMazeId")]
        public long TvMazeId { get; private set; }

        [JsonProperty("tvRageId")]
        public long TvRageId { get; private set; }

        [JsonProperty("useSceneNumbering")]
        public bool UsesSceneNumbering { get; private set; }

        [JsonProperty("year")]
        public int Year { get; private set; }

        #endregion
    }
}