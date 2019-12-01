using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class that defines a response from the "/profile" endpoint.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class QualityProfile : BaseResult, IComparable<QualityProfile>
    {
        [JsonProperty("Cutoff")]
        public QualityDetails Cutoff { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("items")]
        public QualityItemCollection Items { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        public int CompareTo(QualityProfile other) => this.Id.CompareTo(other.Id);
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class QualityItem : BaseResult, IComparable<QualityItem>
    {
        [JsonProperty("allowed")]
        public bool Allowed { get; set; }

        [JsonProperty("quality")]
        public QualityDetails Quality { get; set; }

        public int CompareTo(QualityItem other) => this.Quality.CompareTo(other.Quality);
    }
}
