using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using System;

namespace MG.Sonarr.Results
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class Quality : BaseResult, IComparable<Quality>, IEquatable<Quality>, IQuality
    {
        #region JSON PROPERTIES
        [JsonProperty("id", Order = 1)]
        public int Id { get; private set; }

        [JsonProperty("name", Order = 2)]
        public string Name { get; private set; }

        [JsonProperty("resolution", Order = 4)]
        public int Resolution { get; private set; }

        [JsonProperty("source", Order = 3)]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public QualitySource Source { get; private set; }

        [JsonConstructor]
        private Quality() { }

        public int CompareTo(Quality x) => this.Id.CompareTo(x.Id);
        public int CompareTo(IQuality other) => this.Id.CompareTo(other.Id);
        public bool Equals(Quality other) => this.Id.Equals(other.Id);
        public bool Equals(IQuality other) => this.Id.Equals(other.Id);

        #endregion
    }
}