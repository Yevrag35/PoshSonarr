using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptOut)]
    public class LogRecord
    {
        #region JSON PROPERTIES
        [JsonProperty("id")]
        public long Id { get; private set; }

        [JsonProperty("level")]
        public LogLevel Level { get; private set; }

        [JsonProperty("logger")]
        public string LogSource { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        [JsonProperty("time")]
        public DateTime Time { get; private set; }

        #endregion
    }
}