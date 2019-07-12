using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class SeasonStatistics : SonarrResult
    {
        internal override string[] SkipThese => null;

        public long EpisodeCount { get; internal set; }
        public long EpisodeFileCount { get; internal set; }
        public long PercentOfEpisodes { get; internal set; }
        public long TotalEpisodeCount { get; internal set; }
        public object SizeOnDisk { get; internal set; }

        public SeasonStatistics() : base() { }

        public static explicit operator SeasonStatistics(JObject job)
        {
            var fo = FromJObject<SeasonStatistics>(job);
            var readableSize = fo.MakeReadableDiskSize(Convert.ToInt64(fo.SizeOnDisk));
            fo.SizeOnDisk = readableSize;
            return fo;
        }
            
        private string MakeReadableDiskSize(long sizeOnDisk)
        {
            var div = 1073741824.00M;
            decimal baseVal = sizeOnDisk * 1.00M;
            decimal outVal = Math.Round(baseVal / div, 2);
            return Convert.ToString(outVal) + " GB";
        }
    }
}
