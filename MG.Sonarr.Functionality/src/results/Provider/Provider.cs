using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class MessageProvider : ProviderBase
    {
        #region JSON PROPERTIES

        [JsonProperty("providerMessage", Order = 8)]
        public virtual ProviderMessage Message { get; protected private set; }

        //[JsonProperty("tags", Order = 9)]
        //public virtual int[] Tags { get; protected set; }

        //public abstract string GetEndpoint();

        #endregion
    }
}