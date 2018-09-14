using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class SeasonStatistics
    {
        private long _episodeCount;
        private long _episodeFileCount;
        private long _percentOfEpisodes;
        private long _sizeOnDisk;
        private long _totalEpisodeCount;

        public long EpisodeCount => _episodeCount;
        public long EpisodeFileCount => _episodeFileCount;
        public long PercentOfEpisodes => _percentOfEpisodes;
        public long TotalEpisodeCount => _totalEpisodeCount;
        public string SizeOnDisk
        {
            get
            {
                long div = 1073741824;
                double outVal = (double)(_sizeOnDisk / div);
                return Convert.ToString(outVal) + " GB";
            }
        }

        internal protected SeasonStatistics(JObject job)
        {
            if (job != null)
            { 
                _episodeCount = (long)job["episodeCount"];
                _episodeFileCount = (long)job["episodeFileCount"];
                _percentOfEpisodes = (long)job["percentOfEpisodes"];
                _sizeOnDisk = (long)job["sizeOnDisk"];
                _totalEpisodeCount = (long)job["totalEpisodeCount"];
            }
        }

        public static explicit operator SeasonStatistics(JObject job) => new SeasonStatistics(job);
    }
}
