using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class AllowedQuality : BaseResult, IComparable<AllowedQuality>, IEquatable<AllowedQuality>, IQuality
    {
        [JsonProperty("allowed")]
        public bool Allowed { get; set; }

        [JsonIgnore]
        public int Id => _quality.Id;

        [JsonIgnore]
        public string Name => _quality.Name;

        [JsonIgnore]
        public int Resolution => _quality.Resolution;

        [JsonIgnore]
        public QualitySource Source => _quality.Source;

        [JsonProperty("quality")]
        [JsonConverter(typeof(QualityConverter))]
        private IQuality _quality;

        [JsonConstructor]
        private AllowedQuality() { }
        private AllowedQuality(IQuality quality)
        {
            this._quality = quality;
        }

        public int CompareTo(AllowedQuality other) => this._quality.CompareTo(other._quality);
        public int CompareTo(IQuality other) => this.Id.CompareTo(other.Id);
        public bool Equals(AllowedQuality other)
        {
            return this.Allowed.Equals(other.Allowed) && this._quality.Equals(other._quality);
        }
        public bool Equals(IQuality other)
        {
            return this.Id.Equals(other.Id);
        }

        public static explicit operator AllowedQuality(Quality quality) => new AllowedQuality(quality);
        public static AllowedQuality FromQuality(IQuality quality, bool allowed) => new AllowedQuality(quality) { Allowed = allowed };
    }
}
