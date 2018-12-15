using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace Sonarr.Api.Results
{
    public class SonarrSeriesImage : SonarrResult
    {
        internal override string[] SkipThese => null;

        public string CoverType { get; internal set; }
        public string Url { get; internal set; }

        public SonarrSeriesImage() : base() { }

        public static explicit operator SonarrSeriesImage(JObject job) =>
            FromJObject<SonarrSeriesImage>(job);
    }
}
