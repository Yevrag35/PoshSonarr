using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;

namespace MG.Sonarr.Results
{
    public abstract class ProviderBase : BaseResult
    {
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

        [JsonProperty("name", Order = 2)]
        public string Name { get; set; }
    }
}
