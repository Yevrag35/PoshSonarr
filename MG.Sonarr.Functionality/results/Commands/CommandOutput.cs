using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class defining a response from the "/command" endpoint.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class CommandOutput : BaseResult, IAdditionalInfo, ICommandOutput
    {
        [JsonExtensionData]
        protected private IDictionary<string, JToken> _additionalData;

        [JsonProperty("name")]
        public string Command { get; private set; }

        [JsonProperty("manual")]
        public bool IsManual { get; private set; }

        [JsonProperty("id")]
        public long Id { get; private set; }

        [JsonProperty("priority")]
        public CommandPriority Priority { get; private set; }

        [JsonProperty("queued")]
        [JsonConverter(typeof(UtcOffsetConverter))]
        public DateTimeOffset? Queued { get; private set; }

        [JsonProperty("sendUpdatesToClient")]
        public bool SendUpdatesToClient { get; private set; }

        [JsonProperty("startedOn")]
        [JsonConverter(typeof(UtcOffsetConverter))]
        public DateTimeOffset? Started { get; private set; }

        [JsonProperty("stateChangeTime")]
        [JsonConverter(typeof(UtcOffsetConverter))]
        public DateTimeOffset? StateChangeTime { get; private set; }

        [JsonProperty("status")]
        public CommandStatus Status { get; private set; }

        [JsonProperty("state")]
        public string State { get; private set; }

        [JsonProperty("trigger")]
        public CommandTrigger Trigger { get; private set; }

        [JsonProperty("updateScheduledTask")]
        public bool UpdateScheduledTask { get; private set; }

        public IDictionary GetAdditionalInfo() => _additionalData as IDictionary;
    }
}
