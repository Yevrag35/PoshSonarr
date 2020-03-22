using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class AllowedQuality : BaseResult, IComparable<AllowedQuality>
    {
        [JsonProperty("allowed")]
        public bool Allowed { get; set; }

        [JsonProperty("quality")]
        public Quality Quality { get; internal set; }

        public int CompareTo(AllowedQuality other) => this.Quality.CompareTo(other.Quality);
    }
}
