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
        [JsonProperty("configContract", Order = 5)]
        public string ConfigContract { get; private set; }

        [JsonProperty("fields", Order = 2)]
        public FieldCollection Fields { get; private set; }

        [JsonProperty("implementationName", Order = 3)]
        public string ImplementationName { get; private set; }

        [JsonProperty("implementation", Order = 4)]
        public string Implementation { get; private set; }

        [JsonProperty("infoLink", Order = 6)]
        public Uri InfoLink { get; private set; }

        [JsonProperty("providerMessage", Order = 7)]
        public ProviderMessage Message { get; private set; }

        [JsonProperty("name", Order = 1)]
        public string Name { get; set; }

        [JsonProperty("tags", Order = 8)]
        public virtual int[] Tags { get; protected set; }

        public abstract string GetEndpoint();

        #endregion
    }
}