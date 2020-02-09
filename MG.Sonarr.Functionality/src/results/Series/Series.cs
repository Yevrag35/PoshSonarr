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
    /// The class that defines a response from the "/series" or "/series/lookup" endpoints.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [Serializable]
    public class SeriesResult : SearchSeries, ISupportsTagUpdate
    {
        private const string AIRTIME = "airTime";
        private const string RATING = "ratings";
        private const string EP = "/series";

        //[JsonExtensionData]
        //private IDictionary<string, JToken> _additionalData;

        [JsonIgnore]
        object ISupportsTagUpdate.Id => this.Id;

        [JsonProperty("alternateTitles")]
        public AlternateTitle[] AlternateTitles { get; private set; }

        [JsonProperty("id")]
        public int Id { get; private set; }

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

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("qualityProfileId")]
        public int QualityProfileId { get; set; }

        [JsonProperty("remotePoster")]
        public string RemotePoster { get; private set; }

        [JsonProperty("tags")]
        public HashSet<int> Tags { get; set; }

        [JsonProperty("seasonFolder")]
        public bool UsingSeasonFolders { get; set; }

        public string GetEndpoint() => EP;

        public decimal GetTotalFileSize() => this.Seasons.GetTotalFileSize();
    }
}
