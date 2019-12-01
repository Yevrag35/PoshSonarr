using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class EpisodeQueueItem : BaseEpisodeResult, IComparable<EpisodeQueueItem>
    {
        [JsonProperty("episodeFileId")]
        public long EpisodeFileId { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("lastSearchTime")]
        public DateTime LastSearchTime { get; set; }

        [JsonProperty("overview")]
        public string Overview { get; set; }

        [JsonProperty("sceneEpisodeNumber")]
        public int SceneEpisodeNumber { get; set; }

        [JsonProperty("sceneSeasonNumber")]
        public int SceneSeasonNumber { get; set; }

        public int CompareTo(EpisodeQueueItem other) => this.Id.CompareTo(other.Id);
    }
}
