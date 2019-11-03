using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class that defines the response from the "/queue" endpoint.
    /// </summary>
    public class QueueItem : BaseResult
    {
        public string DownloadId { get; set; }
        public EpisodeQueueItem Episode { get; set; }
        public DateTime? EstimatedCompletionTime { get; set; }
        public long Id { get; set; }
        public ItemQuality Quality { get; set; }
        public string Protocol { get; set; }
        public SeriesResult Series { get; set; }
        public decimal Size { get; set; }
        public decimal SizeLeft { get; set; }
        public string Status { get; set; }
        public string[] StatusMessages { get; set; }
        public TimeSpan? TimeLeft { get; set; }
        public string TrackedDownloadStatus { get; set; }
    }
}
