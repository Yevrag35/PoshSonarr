using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [Serializable]
    public class Season : BaseResult, IComparable<Season>
    {
        [JsonProperty("monitored")]
        public bool IsMonitored { get; set; }

        [JsonProperty("seasonNumber")]
        public int SeasonNumber { get; set; }

        [JsonProperty("statistics")]
        public Statistics Statistics { get; set; }

        public int CompareTo(Season other) => this.SeasonNumber.CompareTo(other.SeasonNumber);
        public bool ShouldSerializeStatistics() => false;

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
    public class Statistics : BaseResult
    {
        [JsonProperty("episodeCount")]
        public int EpisodeCount { get; set; }

        [JsonProperty("episodeFileCount")]
        public int EpisodeFileCount { get; set; }

        [JsonProperty("percentOfEpisodes")]
        public decimal PercentOfEpisodes { get; set; }

        [JsonProperty("previousAiring")]
        public DateTime? PreviousAiring { get; set; }

        [JsonProperty("sizeOnDisk")]
        public decimal SizeOnDisk { get; set; }

        [JsonProperty("totalEpisodeCount")]
        public int TotalEpisodeCount { get; set; }
    }
}
