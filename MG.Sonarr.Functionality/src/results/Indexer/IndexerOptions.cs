using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class IndexerOptions : BaseResult
    {
        #region JSON PROPERTIES
        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonProperty("maximumSize")]
        public int MaximumSizeInGB { get; set; }

        [JsonProperty("minimumAge")]
        public int MinimumAgeInMins { get; set; }

        [JsonProperty("retention")]
        public int RetentionInDays { get; set; }

        [JsonProperty("rssSyncInterval")]
        public int RssSyncIntervalInMins { get; set; }

        #endregion
    }
}