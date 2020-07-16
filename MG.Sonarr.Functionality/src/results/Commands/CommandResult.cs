using MG.Sonarr.Functionality.Converters;
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
        [JsonProperty("duration")]
        public TimeSpan? Duration { get; private set; }

        [JsonProperty("ended")]
        [JsonConverter(typeof(UtcOffsetConverter))]
        public DateTimeOffset? Ended { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        public IDictionary GetAdditionalInfo()
        {
            return _additionalData as IDictionary;
        }

        //[OnDeserialized]
        //private void OnDeserialized(StreamingContext context)
        //{
        //    JToken token = _additionalData["body"].SelectToken("$.completionMessage");
        //    if (token != null)
        //    {
        //        this.Message = token.ToObject<string>();
        //    }
        //}
    }
}
