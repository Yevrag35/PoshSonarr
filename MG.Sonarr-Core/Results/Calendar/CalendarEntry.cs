using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    public class CalendarEntry : BaseResult
    {
        public int AbsoluteEpisodeNumber { get; set; }
        public DateTime? AirDateUtc { get ; set; }
        public int EpisodeFileId { get; set; }
        public int EpisodeNumber { get; set; }
        public bool HasFile { get; set; }
        public long Id { get; set; }
        public bool Monitored { get; set; }
        public int SceneAbsoluteEpisodeNumber { get; set; }
        public int SceneEpisodeNumber { get; set; }
        public int SceneSeasonNumber { get; set; }
        public string Title { get; set; }
        public bool UnverifiedSceneNumbering { get; set; }
        public SeriesResult Series { get; set; }
    }
}
