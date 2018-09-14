using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class SonarrSeason
    {
        private readonly bool _m;
        private readonly long _s;
        private readonly SeasonStatistics _stats;

        public bool IsMonitored => _m;
        public long SeasonNumber => _s;
        public SeasonStatistics Statistics => _stats;

		internal protected SonarrSeason(JObject job)
        {
            _m = (bool)job["monitored"];
            _s = (long)job["seasonNumber"];
            _stats = new SeasonStatistics((JObject)job["statistics"]);
        }

        public static explicit operator SonarrSeason(JObject job) => new SonarrSeason(job);
    }
}
