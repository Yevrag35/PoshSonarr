using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class SonarrSeason : SonarrResult
    {
        private readonly bool _m;
        private readonly long _s;
        private readonly JObject _stats;

        public bool IsMonitored => _m;
        public long SeasonNumber => _s;
        public SeasonStatistics Statistics => (SeasonStatistics)_stats;

		internal protected SonarrSeason(JObject job)
        {
            _m = (bool)job["monitored"];
            _s = (long)job["seasonNumber"];
            _stats = (JObject)job["statistics"];
        }

        public static explicit operator SonarrSeason(JObject job) =>
            job != null ? new SonarrSeason(job) : null;
    }
}
