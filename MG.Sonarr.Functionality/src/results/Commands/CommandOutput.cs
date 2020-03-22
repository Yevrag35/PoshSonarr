using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class defining a response from the "/command" endpoint.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class CommandOutput : BaseResult
    {
        [JsonProperty("name")]
        public string CommandName { get; private set; }

        [JsonProperty("manual")]
        public bool IsManual { get; private set; }

        [JsonProperty("id")]
        public long JobId { get; private set; }

        [JsonProperty("priority")]
        public CommandPriority Priority { get; private set; }

        [JsonProperty("queued")]
        public DateTime Queued { get; private set; }

        [JsonProperty("sendUpdatesToClient")]
        public bool SendUpdatesToClient { get; private set; }

        [JsonProperty("startedOn")]
        public DateTime Started { get; private set; }

        [JsonProperty("stateChangeTime")]
        public DateTime StateChangeTime { get; private set; }

        [JsonProperty("status")]
        public CommandStatus Status { get; private set; }

        [JsonProperty("state")]
        public string State { get; private set; }

        [JsonProperty("trigger")]
        public CommandTrigger Trigger { get; private set; }

        [JsonProperty("updateScheduledTask")]
        public bool UpdateScheduledTask { get; private set; }
    }
}
