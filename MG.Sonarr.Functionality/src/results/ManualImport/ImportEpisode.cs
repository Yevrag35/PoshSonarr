using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ImportEpisode : BaseEpisodeResult
    {
        [JsonProperty("overview")]
        public string Overview { get; private set; }
    }
}
