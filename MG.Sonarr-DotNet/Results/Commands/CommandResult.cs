using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    public class CommandResult : CommandOutput
    {
        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        public TimeSpan Duration { get; set; }
        public DateTime Ended { get; set; }
        public string Message { get; private set; }

        public CommandResult() => _additionalData = new Dictionary<string, JToken>();

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
