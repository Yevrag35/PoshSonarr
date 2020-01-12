using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Metadata : BaseResult, IComparable<Metadata>, IEquatable<Metadata>
    {
        #region JSON PROPERTIES
        [JsonProperty("implementationName", Order = 4)]
        private string _impName;

        [JsonProperty("id", Order = 8)]
        public int Id { get; private set; }

        [JsonProperty("name", Order = 2)]
        public string Name { get; set; }

        [JsonProperty("configContract", Order = 6)]
        public string ConfigContract { get; private set; }

        [JsonProperty("fields", Order = 3)]
        public FieldCollection Fields { get; private set; }

        [JsonProperty("implementation", Order = 5)]
        public string Implementation { get; private set; }

        [JsonProperty("infoLink", Order = 7)]
        public Uri InfoLink { get; private set; }

        [JsonProperty("enable", Order = 1)]
        public bool IsEnabled { get; set; }

        #endregion

        public int CompareTo(Metadata other) => this.Id.CompareTo(other.Id);
        public bool Equals(Metadata other) => this.Id.Equals(other.Id);
    }
}