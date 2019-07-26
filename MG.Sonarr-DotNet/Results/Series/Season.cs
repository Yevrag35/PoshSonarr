using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    public class Season : BaseResult
    {
        public bool Monitored { get; set; }
        public int SeasonNumber { get; set; }
        public Statistics Statistics { get; set; }

        public Season() { }

        internal Season(JToken job, bool fromSearch = false)
        {
            this.Monitored = job["monitored"].ToObject<bool>();
            this.SeasonNumber = job["seasonNumber"].ToObject<int>();
            JToken check = job["statistics"];
            if (check != null)
            {
                this.Statistics = new Statistics(check);
            }
        }

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
                MissingMemberHandling = MissingMemberHandling.Error
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

        public Statistics() { }

        internal Statistics(JToken job)
        {
            this.EpisodeCount = job["episodeCount"].ToObject<int>();
            this.EpisodeFileCount = job["episodeFileCount"].ToObject<int>();
            this.PercentOfEpisodes = job["percentOfEpisodes"].ToObject<decimal>();
            var maybe = job["previousAiring"];
            if (maybe != null)
                this.PreviousAiring = maybe.ToObject<DateTime>();

            this.SizeOnDisk = job["sizeOnDisk"].ToObject<decimal>();
            this.TotalEpisodeCount = job["totalEpisodeCount"].ToObject<int>();
        }
    }
}
