using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class SonarrSeason : SonarrResult
    {
        internal override string[] SkipThese => new string[1] { "Statistics" };

        public bool IsMonitored { get; internal set; }
        public long SeasonNumber { get; internal set; }
        public SeasonStatistics Statistics { get; internal set; }

        public SonarrSeason() : base() { }

        public static explicit operator SonarrSeason(JObject job) =>
            FromJObject<SonarrSeason>(job);
    }
}
