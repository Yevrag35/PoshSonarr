using MG.Sonarr.Functionality;
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
    /// The class that defines a response from the "/series" endpoint.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [Serializable]
    public sealed class SeriesResult : SearchSeries, ICanCalculate, ISupportsTagUpdate
    {
        private const string EP = "/series";

        /// <summary>
        /// An array of alternative titles the series is known as.
        /// </summary>
        [JsonProperty("alternateTitles")]
        public AlternateTitle[] AlternateTitles { get; private set; }

        /// <summary>
        /// The unique ID of the series within Sonarr.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonIgnore]
        object ISupportsTagUpdate.Id => this.Id;

        /// <summary>
        /// Indicates whether the series is monitored for new episodes.
        /// </summary>
        [Obsolete]
        [JsonIgnore]
        public bool Monitored
        {
            get => this.IsMonitored;
            set
            {
                Console.WriteLine("The property \"Monitored\" is deprecated and will be removed from future releases.  Use \"IsMonitored\" instead.");
                this.IsMonitored = value;
            }
        }

        /// <summary>
        /// The containing folder's path for this series.
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary>
        /// The ID of the <see cref="QualityProfile"/> that this series adheres to.
        /// </summary>
        [JsonProperty("qualityProfileId")]
        public int QualityProfileId { get; set; }

        /// <summary>
        /// The remote poster of the series.
        /// </summary>
        [JsonProperty("remotePoster")]
        public string RemotePoster { get; private set; }

        /// <summary>
        /// A unique collection of tag ID's applied to the series.
        /// </summary>
        [JsonProperty("tags")]
        public HashSet<int> Tags { get; set; }

        /// <summary>
        /// Indicates whether the series keeps episode files organized by season number.
        /// </summary>
        [JsonProperty("seasonFolder")]
        public bool UsingSeasonFolders { get; set; }

        public decimal GetTotalFileSize() => base.Seasons.GetTotalFileSize();

        /// <summary>
        /// Retrieves the Uri endpoint that instance was retrieved from.
        /// </summary>
        public string GetEndpoint() => EP;
    }
}
