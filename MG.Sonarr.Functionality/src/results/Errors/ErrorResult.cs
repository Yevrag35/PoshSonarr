using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ErrorResultException : HttpRequestException
    {
        public override string Message => !string.IsNullOrWhiteSpace(this.ErrorResponse)
            ? string.Format("{0}: Property (\"{1}\"); AttemptedValue (\"{2}\")", this.ErrorResponse, this.PropertyName, this.AttemptedValue)
            : string.Format("Unknown error: Property (\"{0}\"); AttemptedValue (\"{1}\")", this.PropertyName, this.AttemptedValue);

        #region JSON PROPERTIES
        [JsonProperty("attemptedValue", Order = 3)]
        public string AttemptedValue { get; private set; }

        [JsonProperty("errorMessage", Order = 2)]
        public string ErrorResponse { get; private set; }

        [JsonProperty("propertyName", Order = 1)]
        public string PropertyName { get; private set; }

        public ErrorResultException() : base()
        {
        }

        #endregion
    }
}