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
    public class Tag : TagNew, IComparable<Tag>, IEquatable<Tag>
    {
        [JsonProperty("id")]
        public int TagId { get; private set; }

        public Tag() : base(null) { }
        public Tag(string label) : base(label) { }

        public int CompareTo(Tag other) => this.TagId.CompareTo(other.TagId);
        public bool Equals(Tag other) => this.TagId.Equals(other.TagId);
        public override int GetHashCode() => this.TagId.GetHashCode();
    }
}
