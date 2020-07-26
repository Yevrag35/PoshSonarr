using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptOut)]
    public class ExceptionLogRecord : LogRecord
    {
        #region JSON PROPERTIES
        [JsonProperty("exception")]
        public string Exception { get; private set; }

        [JsonProperty("exceptionType")]
        public string ExceptionType { get; private set; }

        #endregion
    }
}