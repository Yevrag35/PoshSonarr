using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptOut)]
    public class LogRecord : BaseResult
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
        [JsonConverter(typeof(UtcTimeConverter))]
        public DateTime Time { get; private set; }

        #endregion
    }
}