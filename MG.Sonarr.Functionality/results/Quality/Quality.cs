using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class Quality : BaseResult, IComparable<Quality>, IEquatable<Quality>
    {
        #region JSON PROPERTIES
        [JsonProperty("id", Order = 1)]
        public int Id { get; private set; }

        [JsonProperty("name", Order = 2)]
        public string Name { get; private set; }

        [JsonProperty("resolution", Order = 4)]
        public int Resolution { get; private set; }

        [JsonProperty("source", Order = 3)]
        public string Source { get; private set; } = string.Empty;

        public int CompareTo(Quality x) => this.Id.CompareTo(x.Id);

        [JsonConstructor]
        private Quality() { }

        public bool Equals(Quality other) => this.Id.Equals(other.Id);
        public override int GetHashCode() => this.Id.GetHashCode();

        #endregion
    }
}