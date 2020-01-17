using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Provider : BaseResult
    {
        #region JSON PROPERTIES
        [JsonProperty("configContract", Order = 6)]
        public string ConfigContract { get; protected private set; }

        [JsonProperty("fields", Order = 3)]
        public FieldCollection Fields { get; protected private set; }

        [JsonProperty("implementationName", Order = 3)]
        public string ImplementationName { get; protected private set; }

        [JsonProperty("implementation", Order = 5)]
        public string Implementation { get; protected private set; }

        [JsonProperty("infoLink", Order = 7)]
        public Uri InfoLink { get; protected private set; }

        [JsonProperty("providerMessage", Order = 8)]
        public ProviderMessage Message { get; protected private set; }

        [JsonProperty("name", Order = 2)]
        public string Name { get; set; }

        [JsonProperty("tags", Order = 9)]
        public virtual int[] Tags { get; protected set; }

        public abstract string GetEndpoint();

        #endregion
    }
}