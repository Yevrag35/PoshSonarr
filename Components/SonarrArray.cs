using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sonarr.Api.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sonarr.Api
{
    public class SonarrArray : List<SonarrResult>
    {
        public SonarrArray() : base() { }
        public SonarrArray(IEnumerable<SonarrResult> srs) : base(srs) { }
        public SonarrArray(int capacity) : base(capacity) { }

        public static string ToJson(IEnumerable<SonarrResult> srs)
        {
            var srsArray = srs.ToArray();
            return JsonConvert.SerializeObject(srsArray, Formatting.Indented);
        }
    }
}
