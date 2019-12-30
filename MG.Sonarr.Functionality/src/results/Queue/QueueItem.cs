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
        public string DownloadId { get; set; }

        [JsonProperty("episode")]
        public EpisodeQueueItem Episode { get; set; }

        [JsonProperty("estimatedCompletionTime")]
        public DateTime? EstimatedCompletionTime { get; set; }

        [JsonProperty("id")]
        public long QueueItemId { get; set; }

        [JsonProperty("quality")]
        public ItemQuality Quality { get; set; }

        [JsonProperty("protocol")]
        public string Protocol { get; set; }

        [JsonProperty("series")]
        public SeriesResult Series { get; set; }

        [JsonProperty("size")]
        public decimal Size { get; set; }

        [JsonProperty("sizeLeft")]
        public decimal SizeLeft { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("statusMessages")]
        public string[] StatusMessages { get; set; }

        [JsonProperty("timeLeft")]
        public TimeSpan? TimeLeft { get; set; }

        [JsonProperty("trackedDownloadStatus")]
        public string TrackedDownloadStatus { get; set; }

        public int CompareTo(QueueItem other) => this.QueueItemId.CompareTo(other.QueueItemId);
    }
}
