using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class CommandResult : CommandOutput, IAdditionalInfo
    {
        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        [JsonProperty("duration")]
        public TimeSpan Duration { get; set; }

        [JsonProperty("ended")]
        public DateTime Ended { get; set; }

        public string Message { get; private set; }

        public IDictionary GetAdditionalInfo()
        {
            return (IDictionary)_additionalData;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            JToken token = _additionalData["body"].SelectToken("$.completionMessage");
            if (token != null)
            {
                this.Message = token.ToObject<string>();
            }
        }
    }
}
