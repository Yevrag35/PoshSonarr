using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    [Serializable]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class TagNew : BaseResult
    {
        [JsonProperty("label", Order = 1)]
        public string Label { get; set; }

        public TagNew(string label) => this.Label = label;
    }

    /// <summary>
    /// The class that defines a response from the "/tag" endpoint.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Tag : TagNew
    {
        [JsonProperty("id")]
        public int TagId { get; private set; }

        public Tag() : base(null) { }
        public Tag(string label) : base(label) { }
    }
}
