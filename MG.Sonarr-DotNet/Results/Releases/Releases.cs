using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class that defines a response from the "/release" endpoint.
    /// </summary>
    public class Release : BaseResult
    {
        public int[] AbsoluteEpisodeNumbers { get; set; }
        public int Age { get; set; }
        public decimal AgeHours { get; set; }
        public decimal AgeMinutes { get; set; }
        public bool Approved { get; set; }
        public bool DownloadAllowed { get; set; }
        public Uri DownloadUrl { get; set; }
        public int[] EpisodeNumbers { get; set; }
        public bool FullSeason { get; set; }
        public string Indexer { get; set; }
        public Uri InfoUrl { get; set; }
        public int IndexerId { get; set; }
        public bool IsAbsoluteNumbering { get; set; }
        public bool IsDaily { get; set; }
        public bool IsPossibleSpecialEpisode { get; set; }
        public string Language { get; set; }
        public string Protocol { get; set; }
        public DateTime PublishDate { get; set; }
        public ItemQuality Quality { get; set; }
        public bool Rejected { get; set; }
        public string[] Rejections { get; set; }
        public string ReleaseGroup { get; set; }
        public string ReleaseHash { get; set; }
        public int ReleaseWeight { get; set; }

        [JsonProperty("guid")]
        public Uri ReleaseUrl { get; set; }
        public int SeasonNumber { get; set; }
        public string SeriesTitle { get; set; }
        public long Size { get; set; }
        public bool Special { get; set; }
        public bool TemporarilyRejected { get; set; }
        public string Title { get; set; }
        public string TVDBId { get; set; }
        public string TVRageId { get; set; }
    }
}
