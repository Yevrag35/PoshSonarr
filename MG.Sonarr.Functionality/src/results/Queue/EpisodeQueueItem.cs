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
        public long EpisodeFileId { get; private set; }

        [JsonProperty("id")]
        public long QueueItemId { get; private set; }

        [JsonProperty("lastSearchTime")]
        public DateTime LastSearchTime { get; private set; }

        [JsonProperty("overview")]
        public string Overview { get; private set; }

        [JsonProperty("sceneEpisodeNumber")]
        public int SceneEpisodeNumber { get; private set; }

        [JsonProperty("sceneSeasonNumber")]
        public int SceneSeasonNumber { get; private set; }

        public int CompareTo(EpisodeQueueItem other) => this.QueueItemId.CompareTo(other.QueueItemId);
    }
}
