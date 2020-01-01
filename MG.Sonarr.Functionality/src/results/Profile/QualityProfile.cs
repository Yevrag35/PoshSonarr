using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class QualityProfileNew : BaseResult
    {
        [JsonProperty("cutoff", Order = 2)]
        public QualityDetails Cutoff { get; set; }

        [JsonProperty("items", Order = 3)]
        public QualityItemCollection Qualities { get; set; } = new QualityItemCollection();

        [JsonProperty("language", Order = 4)]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public ProfileLanugage Language { get; set; } = ProfileLanugage.English;

        [JsonProperty("name", Order = 1)]
        public string Name { get; set; }

        public QualityProfileNew() { }
    }

    /// <summary>
    /// The class that defines a response from the "/profile" endpoint.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class QualityProfile : QualityProfileNew, IComparable<QualityProfile>
    {
        [JsonProperty("id", Order = 5)]
        public int Id { get; internal set; }

        public QualityProfile() : base() { }

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
