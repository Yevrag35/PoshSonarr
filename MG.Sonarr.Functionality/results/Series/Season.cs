using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// A class representing a single season of an individual <see cref="SeriesResult"/>.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [Serializable]
    public class Season : SizedResult, IComparable<Season>
    {
        //[JsonExtensionData]
        //private Dictionary<string, JToken> _extData;

        //[JsonIgnore]
        [JsonProperty("statistics")]
        private Statistics _stats;

        /// <summary>
        /// The number of episodes in the season.
        /// </summary>
        [JsonIgnore]
        public int EpisodeCount => _stats.EpisodeCount;

        /// <summary>
        /// The number of episode files found for the season.
        /// </summary>
        [JsonIgnore]
        public int EpisodeFileCount => _stats.EpisodeFileCount;

        /// <summary>
        /// Indicates whether the season is being monitored for new episodes.
        /// This value is superseded by <see cref="SearchSeries.IsMonitored"/>.
        /// </summary>
        [JsonProperty("monitored")]
        public bool IsMonitored { get; set; }

        /// <summary>
        /// The percent of episodes downloaded from the total episodes available.
        /// </summary>
        [JsonIgnore]
        public double PercentOfEpisodes => _stats.PercentOfEpisodes;

        /// <summary>
        /// The localized date of the last airing episode in the season.
        /// </summary>
        [JsonIgnore]
        public DateTime? PreviousAiring => _stats.PreviousAiring;

        /// <summary>
        /// The <see cref="int"/> value for the season number.
        /// </summary>
        [JsonProperty("seasonNumber")]
        public int SeasonNumber { get; private set; }

        /// <summary>
        /// The size (in Bytes) of all downloaded episodes for the season.
        /// </summary>
        [JsonIgnore]
        [JsonConverter(typeof(SizeConverter))]
        public override Size SizeOnDisk => _stats.SizeOnDisk;
        //public override long SizeOnDisk => _stats.SizeOnDisk;

        /// <summary>
        /// The total number of episodes available in the season.
        /// </summary>
        [JsonIgnore]
        public int TotalEpisodeCount => _stats.TotalEpisodeCount;

        public int CompareTo(Season other) => this.SeasonNumber.CompareTo(other.SeasonNumber);

        public override string ToJson()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DefaultValueHandling = DefaultValueHandling.Populate,
                Formatting = Formatting.Indented,
                MaxDepth = 5,
                NullValueHandling = NullValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Ignore
            });
        }
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class Statistics : BaseResult
    {
        [JsonProperty("episodeCount")]
        internal int EpisodeCount { get; private set; }

        [JsonProperty("episodeFileCount")]
        internal int EpisodeFileCount { get; private set; }

        [JsonProperty("percentOfEpisodes")]
        internal double PercentOfEpisodes { get; private set; }

        [JsonProperty("previousAiring")]
        internal DateTime? PreviousAiring { get; private set; }

        [JsonProperty("sizeOnDisk")]
        internal Size SizeOnDisk { get; private set; }
        //internal long SizeOnDisk { get; private set; }

        [JsonProperty("totalEpisodeCount")]
        internal int TotalEpisodeCount { get; private set; }

        [JsonConstructor]
        internal Statistics() { }
    }
}
