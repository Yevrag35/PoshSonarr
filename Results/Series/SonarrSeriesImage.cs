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
        //private readonly string _ct;
        //private readonly Uri _url;

        public string CoverType { get; internal set; }
        public Uri Url { get; internal set; }

        //internal protected SonarrSeriesImage(JObject job)
        //{
        //    if (job != null)
        //    {
        //        _ct = (string)job["coverType"];
        //        _url = new Uri((string)job["url"], UriKind.RelativeOrAbsolute);
        //    }
        //}

        //public static explicit operator SonarrSeriesImage(JObject job) =>
        //    job != null ? new SonarrSeriesImage(job) : null;
    }
}
