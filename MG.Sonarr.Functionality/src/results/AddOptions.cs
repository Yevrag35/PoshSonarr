using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    [Serializable]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class AddOptions : BaseResult
    {
        [JsonProperty("ignoreEpisodesWithFiles")]
        public bool IgnoreEpisodesWithFiles { get; set; } = true;

        [JsonProperty("ignoreEpisodesWithoutFiles")]
        public bool IgnoreEpisodesWithoutFiles { get; set; } = true;

        [JsonProperty("searchForMissingEpisodes")]
        public bool SearchForMissingEpisodes { get; set; }
    }
}
