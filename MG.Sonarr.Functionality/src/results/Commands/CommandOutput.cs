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
        public string CommandName { get; set; }

        [JsonProperty("manual")]
        public bool IsManual { get; set; }

        [JsonProperty("id")]
        public long JobId { get; set; }

        [JsonProperty("priority")]
        public CommandPriority Priority { get; set; }

        [JsonProperty("queued")]
        public DateTime Queued { get; set; }

        [JsonProperty("sendUpdatesToClient")]
        public bool SendUpdatesToClient { get; set; }

        [JsonProperty("startedOn")]
        public DateTime Started { get; set; }

        [JsonProperty("stateChangeTime")]
        public DateTime StateChangeTime { get; set; }

        [JsonProperty("status")]
        public CommandStatus Status { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("trigger")]
        public CommandTrigger Trigger { get; set; }

        [JsonProperty("updateScheduledTask")]
        public bool UpdateScheduledTask { get; set; }
    }
}
