using Newtonsoft.Json;
using System;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class that defines the response from the "/queue" endpoint.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class QueueItem : BaseResult, IComparable<QueueItem>
    {
        [JsonProperty("downloadId")]
        public string DownloadId { get; private set; }

        [JsonProperty("episode")]
        public EpisodeQueueItem Episode { get; private set; }

        [JsonProperty("estimatedCompletionTime")]
        public DateTime? EstimatedCompletionTime { get; private set; }

        [JsonProperty("id")]
        public long QueueItemId { get; private set; }

        [JsonProperty("quality")]
        public ItemQuality Quality { get; private set; }

        [JsonProperty("protocol")]
        public string Protocol { get; private set; }

        [JsonProperty("series")]
        public SeriesResult Series { get; private set; }

        [JsonProperty("size")]
        public decimal Size { get; private set; }

        [JsonProperty("sizeLeft")]
        public decimal SizeLeft { get; private set; }

        [JsonProperty("status")]
        public string Status { get; private set; }

        [JsonProperty("statusMessages")]
        public string[] StatusMessages { get; private set; }

        [JsonProperty("timeLeft")]
        public TimeSpan? TimeLeft { get; private set; }

        [JsonProperty("trackedDownloadStatus")]
        public string TrackedDownloadStatus { get; private set; }

        public int CompareTo(QueueItem other) => this.QueueItemId.CompareTo(other.QueueItemId);
    }
}
