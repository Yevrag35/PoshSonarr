using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [Serializable]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class QualityDefinition : BaseResult
    {
        [JsonProperty("maxSize", Order = 5)]
        public double MaxSize { get; set; } = 100.0d;

        [JsonProperty("minSize", Order = 4)]
        public double MinSize { get; set; } = 0.0d;

        [JsonProperty("quality", Order = 1)]
        public Quality Quality { get; private set; }

        [JsonProperty("id", Order = 6)]
        public int QualityDefinitionId { get; private set; }

        [JsonProperty("title", Order = 2)]
        public string Title { get; set; }

        [JsonProperty("weight", Order = 3)]
        public int Weight { get; set; } = 1;

        public QualityDefinition() { }

        public void SetQuality(Quality quality) => this.Quality = quality;
    }
}
