using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ProviderMessage
    {
        #region JSON PROPERTIES
        [JsonProperty("message")]
        public string Message { get; private set; }

        [JsonProperty("type")]
        public string Type { get; private set; } = string.Empty;

        #endregion
    }
}