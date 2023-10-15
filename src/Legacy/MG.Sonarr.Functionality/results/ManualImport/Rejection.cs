using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Rejection : BaseResult
    {
        #region JSON PROPERTIES
        [JsonProperty("reason")]
        public string Reason { get; private set; }

        [JsonProperty("type", Order = 2)]
        public string Type { get; private set; } = string.Empty;

        #endregion
    }
}