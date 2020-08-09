using MG.Sonarr.Functionality.Converters;
using MG.Sonarr.Results;
using MG.Sonarr.Results.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class RecordPageBase : BaseResult
    {
        #region JSON PROPERTIES
        [JsonProperty("page", Order = 1)]
        public int Page { get; private set; }

        [JsonProperty("pageSize", Order = 2)]
        public int PageSize { get; private set; }

        [JsonProperty("sortDirection", Order = 4)]
        public SortDirection SortDirection { get; private set; }

        [JsonProperty("sortKey", Order = 3)]
        public string SortKey { get; private set; }

        [JsonProperty("totalRecords", Order = 5)]
        public int TotalRecords { get; private set; }

        #endregion

    }
}