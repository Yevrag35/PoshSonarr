using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class SeasonStatistics : SonarrResult
    {
        internal override string[] SkipThese => new string[1]
        {
            "SizeOnDisk"
        };

        public long EpisodeCount { get; internal set; }
        public long EpisodeFileCount { get; internal set; }
        public long PercentOfEpisodes { get; internal set; }
        public long TotalEpisodeCount { get; internal set; }
        //public string SizeOnDisk
        //{
        //    get
        //    {
        //        long div = 1073741824;
        //        double outVal = (double)(_sizeOnDisk / div);
        //        return Convert.ToString(outVal) + " GB";
        //    }
        //}

        public SeasonStatistics() : base() { }

        public static explicit operator SeasonStatistics(JObject job) =>
            FromJObject<SeasonStatistics>(job);
    }
}
