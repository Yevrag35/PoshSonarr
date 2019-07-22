using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Results
{
    public class EpisodeQueueItem : BaseEpisodeResult
    {
        public long EpisodeFileId { get; set; }
        public long Id { get; set; }
        public DateTime LastSearchTime { get; set; }
        public string Overview { get; set; }
        public int SceneEpisodeNumber { get; set; }
        public int SceneSeasonNumber { get; set; }
    }
}
