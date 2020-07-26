using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RecordSummary
    {
        #region JSON PROPERTIES
        [JsonProperty("page", Order = 1)]
        public int Page { get; private set; }

        [JsonProperty("pageSize", Order = 2)]
        public int PageSize { get; private set; }

        [JsonProperty("sortKey")]
        public string SortKey { get; private set; }

        [JsonProperty("sortDirection")]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public SortDirection SortDirection { get; private set; }

        [JsonProperty("totalRecords")]
        public int TotalRecords { get; private set; }

        [JsonProperty("records", ItemConverterType = typeof(RecordConverter))]
        public RecordCollection Records { get; private set; }

        #endregion
    }
}