using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Results
{
    public class EpisodeFile : BaseResult
    {
        public DateTime? DateAdded { get; set; }

        [JsonProperty("Id")]
        public long EpisodeFileId { get; set; }

        public MediaInfo MediaInfo { get; set; }
        public string OriginalFilePath { get; set; }
        public string Path { get; set; }
        public EpisodeFileQuality Quality { get; set; }
        public bool QualityCutoffNotMet { get; set; }
        public string RelativePath { get; set; }
        public string SceneName { get; set; }
        public int SeriesId { get; set; }
        public int SeasonNumber { get; set; }
        public long Size { get; set; }
    }

    public class EpisodeFileQuality : BaseResult
    {
        public QualityDetails Quality { get; set; }
        public Revision Revision { get; set; }
    }
}
