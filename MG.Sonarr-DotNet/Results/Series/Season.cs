using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [Serializable]
    public class Season : BaseResult
    {
        public bool Monitored { get; set; }
        public int SeasonNumber { get; set; }
        public Statistics Statistics { get; set; }

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

    public class Statistics : BaseResult
    {
        public int EpisodeCount { get; set; }
        public int EpisodeFileCount { get; set; }
        public decimal PercentOfEpisodes { get; set; }
        public DateTime? PreviousAiring { get; set; }
        public decimal SizeOnDisk { get; set; }
        public int TotalEpisodeCount { get; set; }
    }
}
