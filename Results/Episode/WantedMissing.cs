using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sonarr.Api.Components;
using Sonarr.Api.Endpoints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sonarr.Api.Results
{
    public class WantedMissingResult : SonarrResult
    {
        internal override string[] SkipThese => null;

        public int Page { get; internal set; }
        public int PageSize { get; internal set; }
        public string SortKey { get; internal set; }
        public string SortDirection { get; internal set; }
        public int TotalRecords { get; internal set; }
        public SonarrArray<Episode> Records { get; internal set; }

        public WantedMissingResult() { }
    }
}
