using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization = MemberSerialization.OptOut, ItemNullValueHandling = NullValueHandling.Ignore)]
    public class SeriesPost : BaseResult, IGetEndpoint
    {
        private const string EP = "/series";

        [JsonProperty("rootFolderPath", Order = 4)]
        private string _rootFolder;

        [JsonProperty("path", Order = 4)]
        private string _fullPath;

        [JsonProperty("images", Order = 1)]
        public List<SeriesImage> Images { get; set; }

        [JsonIgnore]
        public bool IsFullPath { get; set; }

        [JsonProperty("monitored", Order = 2)]
        public bool IsMonitored { get; set; }

        [JsonProperty("title", Order = 7)]
        public string Name { get; set; }

        [JsonIgnore]
        public string Path
        {
            get
            {
                if (this.IsFullPath)
                    return _fullPath;

                else
                    return _rootFolder;
            }
            set
            {
                if (this.IsFullPath)
                    _fullPath = value;

                else
                    _rootFolder = value;
            }
        }

        [JsonProperty("addOptions", Order = 12)]
        public AddOptions Options { get; set; }

        [JsonProperty("qualityProfileId", Order = 3)]
        public int QualityProfileId { get; set; }

        [JsonProperty("seasons", Order = 6)]
        public SeasonCollection Seasons { get; set; }

        [JsonProperty("seriesType")]
        public string SeriesType { get; set; } = string.Empty;

        [JsonProperty("tags")]
        public HashSet<int> Tags { get; set; }

        /// <summary>
        /// The slug title of the series.
        /// </summary>
        [JsonProperty("titleSlug", Order = 8)]
        public string TitleSlug { get; set; }

        /// <summary>
        /// The TVDB ID of the series.
        /// </summary>
        [JsonProperty("tvdbId", Order = 9)]
        public long TVDBId { get; set; }

        /// <summary>
        /// The TVMaze ID of the series.
        /// </summary>
        [JsonProperty("tvMazeId", Order = 10)]
        public long? TvMazeId { get; set; }

        /// <summary>
        /// The TVRage ID of the series.
        /// </summary>
        [JsonProperty("tvRageId", Order = 11)]
        public long? TvRageId { get; set; }

        [JsonProperty("seasonFolder", Order = 5)]
        public bool UsingSeasonFolders { get; set; }

        public SeriesPost() : base()
        {
        }

        public string GetEndpoint() => EP;

        public static SeriesPost NewPost(SearchSeries searchResult)
        {
            return new SeriesPost
            {
                Images = new List<SeriesImage>(searchResult.Images),
                IsMonitored = searchResult.IsMonitored,
                Name = searchResult.Name,
                Options = new AddOptions(),
                Seasons = searchResult.Seasons,
                SeriesType = searchResult.SeriesType,
                TitleSlug = searchResult.TitleSlug,
                TVDBId = searchResult.TVDBId,
                TvMazeId = searchResult.TvMazeId,
                TvRageId = searchResult.TvRageId
            };
        }
    }
}