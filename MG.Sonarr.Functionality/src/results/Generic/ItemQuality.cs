using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ItemQuality : BaseResult
    {
        [JsonProperty("quality")]
        public QualityDetails Quality { get; private set; }

        [JsonProperty("revision")]
        public Revision Revision { get; private set; }
    }
}
