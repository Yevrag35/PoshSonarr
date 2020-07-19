using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class NameId : BaseResult, ISeries
    {
        #region JSON PROPERTIES
        [JsonProperty("title")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        #endregion
    }
}