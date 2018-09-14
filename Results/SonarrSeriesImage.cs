using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace Sonarr.Api.Results
{
    public class SonarrSeriesImage
    {
        private readonly string _ct;
        private readonly Uri _url;

        public string CoverType => _ct;
        public Uri Url => _url;

        internal protected SonarrSeriesImage(JObject job)
        {
            _ct = (string)job["coverType"];
            _url = new Uri((string)job["url"], UriKind.RelativeOrAbsolute);
        }
    }
}
