using MG.Sonarr.Functionality;
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
    public class CommandResult : CommandOutput, ICommandResult
    {
        [JsonProperty("duration")]
        public TimeSpan? Duration { get; private set; }

        [JsonProperty("ended")]
        [JsonConverter(typeof(UtcOffsetConverter))]
        public DateTimeOffset? Ended { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }
    }
}
