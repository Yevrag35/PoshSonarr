using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class QualityProfileNew : BaseResult, IGetEndpoint
    {
        private const string EP = "/profile";

        [JsonProperty("cutoff", Order = 2)]
        public Quality Cutoff { get; set; }

        //[JsonProperty("items", Order = 3)]
        //public QualityItemCollection Qualities { get; set; } = new QualityItemCollection();

        [JsonProperty("items", Order = 3)]
        public AllowedQualityCollection AllowedQualities { get; private set; } = new AllowedQualityCollection();

        [JsonProperty("language", Order = 4)]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public ProfileLanugage Language { get; set; } = ProfileLanugage.English;

        [JsonProperty("name", Order = 1)]
        public string Name { get; set; }

        public QualityProfileNew() { }

        public bool IsQualityAllowed(string name, bool caseSensitive = false)
        {
            bool? result = this.AllowedQualities.GetAllowedQualityByName(name, caseSensitive)?.Allowed;
            if (!result.HasValue)
                result = false;

            return result.Value;
        }
        public bool IsQualityAllowed(int qualityId)
        {
            bool? result = this.AllowedQualities.GetAllowedQualityById(qualityId)?.Allowed;
            if (!result.HasValue)
                result = false;

            return result.Value;
        }

        public string GetEndpoint() => EP;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx) => this.AllowedQualities.Sort();
    }

    /// <summary>
    /// The class that defines a response from the "/profile" endpoint.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class QualityProfile : QualityProfileNew, IComparable<QualityProfile>, IEquatable<QualityProfile>
    {
        [JsonProperty("id", Order = 5)]
        public int Id { get; internal set; }

        public QualityProfile() : base() { }

        public int CompareTo(QualityProfile other) => this.Id.CompareTo(other.Id);
        public bool Equals(QualityProfile other) => this.Id.Equals(other.Id);
    }
}
