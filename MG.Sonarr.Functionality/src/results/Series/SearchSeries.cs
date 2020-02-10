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
    /// <summary>
    /// The class that defines a response from the "/series/lookup" endpoint.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class SearchSeries : BaseResult, IGetEndpoint
    {
        private const string EP = "/series";

        #region JSON PROPERTIES

        /// <summary>
        /// Represents the date when the series was added.
        /// </summary>
        [JsonProperty("added")]
        public DateTime Added { get; private protected set; }

        /// <summary>
        /// The localized date of when the series aires.
        /// </summary>
        [JsonProperty("airTime")]
        public string AirTime { get; private protected set; }

        /// <summary>
        /// The cleaned title of the series.
        /// </summary>
        [JsonProperty("cleanTitle")]
        public string CleanTitle { get; private protected set; }

        /// <summary>
        /// The localized date of when the series first aired.
        /// </summary>
        [JsonProperty("firstAired")]
        public DateTime FirstAired { get; private protected set; }

        /// <summary>
        /// An array of genres that apply to the series.
        /// </summary>
        [JsonProperty("genres")]
        public string[] Genres { get; private protected set; }

        /// <summary>
        /// An array of images for the series.
        /// </summary>
        [JsonProperty("images")]
        public SeriesImage[] Images { get; private protected set; }

        /// <summary>
        /// The IMDB ID for the series.
        /// </summary>
        [JsonProperty("imdbId")]
        public string IMDBId { get; private protected set; }

        /// <summary>
        /// Indicates whether the series is monitored for new episodes.
        /// </summary>
        [JsonProperty("monitored")]
        public bool IsMonitored { get; set; }

        /// <summary>
        /// The standard title of the series.
        /// </summary>
        [JsonProperty("title")]
        public string Name { get; private protected set; }

        /// <summary>
        /// The TV network that airs (currently or previously) the series.
        /// </summary>
        [JsonProperty("network")]
        public string Network { get; private protected set; }

        /// <summary>
        /// A short synopsis of the series.
        /// </summary>
        [JsonProperty("overview")]
        public string Overview { get; private protected set; }

        /// <summary>
        /// The audience rating of the series.
        /// </summary>
        [JsonIgnore]
        public float Rating => _ratings.Value;

        [JsonProperty("ratings")]
        internal Ratings _ratings;

        /// <summary>
        /// Indicates how many votes were cast for the rating.
        /// </summary>
        [JsonIgnore]
        public long RatingVotes => _ratings.Votes;

        /// <summary>
        /// The Uri of the series's poster.
        /// </summary>
        [JsonProperty("remotePoster")]
        public string PosterUrl { get; private protected set; }

        /// <summary>
        /// Represents a single episode's runtime duration for the series.
        /// </summary>
        [JsonProperty("runtime")]
        public int Runtime { get; private protected set; }

        /// <summary>
        /// Indicates how seasons are available for the series.
        /// </summary>
        [JsonProperty("seasonCount")]
        public int SeasonCount { get; private protected set; }

        /// <summary>
        /// A collection of <see cref="Season"/>'s for the series.
        /// </summary>
        [JsonProperty("seasons")]
        public SeasonCollection Seasons { get; private protected set; }

        /// <summary>
        /// Indicates the type of series.
        /// </summary>
        [JsonProperty("seriesType")]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public SeriesType SeriesType { get; set; }

        /// <summary>
        /// The title of the series used when sorting or filtering.
        /// </summary>
        [JsonProperty("sortTitle")]
        public string SortTitle { get; private protected set; }

        /// <summary>
        /// Indicates the current status of the series.
        /// </summary>
        [JsonProperty("status")]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public SeriesStatusType Status { get; private protected set; }

        /// <summary>
        /// The slug title of the series.
        /// </summary>
        [JsonProperty("titleSlug")]
        public string TitleSlug { get; private protected set; }

        /// <summary>
        /// The TVDB ID of the series.
        /// </summary>
        [JsonProperty("tvdbId")]
        public long TVDBId { get; private protected set; }

        /// <summary>
        /// The TVMaze ID of the series.
        /// </summary>
        [JsonProperty("tvMazeId")]
        public long TvMazeId { get; private protected set; }

        /// <summary>
        /// The TVRage ID of the series.
        /// </summary>
        [JsonProperty("tvRageId")]
        public long TvRageId { get; private protected set; }

        /// <summary>
        /// The TV Parental Guidelines rating for the series.
        /// </summary>
        [JsonProperty("certification")]
        public string TvRating { get; private protected set; }

        /// <summary>
        /// Indicates if the series uses scene numbering.
        /// </summary>
        [JsonProperty("useSceneNumbering")]
        public bool UsesSceneNumbering { get; private protected set; }

        /// <summary>
        /// The year the series first aired.
        /// </summary>
        [JsonProperty("year")]
        public int Year { get; private protected set; }

        /// <summary>
        /// Initializes a new instance of <see cref="SearchSeries"/>.
        /// </summary>
        [JsonConstructor]
        public SearchSeries() { }

        #endregion

        public string GetEndpoint() => EP;
    }
}