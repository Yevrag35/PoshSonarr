using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class that defines a response from the "/tag" endpoint.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Tag : BaseResult
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("id")]
        public int TagId { get; private set; }
    }
}
