using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    public class Season
    {
        public bool Monitored { get; }
        public int SeasonNumber { get; }
        public Statistics Statistics { get; }

        internal Season(JToken job)
        {
            this.Monitored = job["monitored"].ToObject<bool>();
            this.SeasonNumber = job["seasonNumber"].ToObject<int>();
            this.Statistics = new Statistics(job["statistics"]);
        }
    }

    public class Statistics
    {
        public int EpisodeCount { get; }
        public int EpisodeFileCount { get; }
        public decimal PercentOfEpisodes { get; }
        public DateTime? PreviousAiring { get; }
        public decimal SizeOnDisk { get; }
        public int TotalEpisodeCount { get; }

        public Statistics(JToken job)
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
