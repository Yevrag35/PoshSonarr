using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    public class CommandOutput : BaseResult
    {
        [JsonProperty("name")]
        public string CommandName { get; set; }
        [JsonProperty("manual")]
        public bool IsManual { get; set; }
        [JsonProperty("id")]
        public long JobId { get; set; }
        public string Priority { get; set; }
        public DateTime Queued { get; set; }
        public bool SendUpdatesToClient { get; set; }
        [JsonProperty("startedOn")]
        public DateTime Started { get; set; }
        public DateTime StateChangeTime { get; set; }
        public string Status { get; set; }
        public string State { get; set; }
        public string Trigger { get; set; }
        public bool UpdateScheduledTask { get; set; }
    }
}
