using MG.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class CalendarEntry : SonarrResult
    {
        internal override string[] SkipThese => new string[1] { "AirDate" };

        public long SeriesId { get; internal set; }
        public long EpisodeFileId { get; internal set; }
        public long SeasonNumber { get; internal set; }
        public long EpisodeNumber { get; internal set; }
        public string Title { get; internal set; }
        public DateTime? AirDate =>
            AirDateUtc.HasValue ? AirDateUtc.Value.ToLocalTime() : (DateTime?)null;
        public DateTime? AirDateUtc { get; internal set; }
        public EpisodeFile EpisodeFile { get; internal set; }
        public bool HasFile { get; internal set; }
        public bool IsMonitored { get; internal set; }
        public long AbsoluteEpisodeNumber { get; internal set; }
        public bool UnverifiedSceneNumbering { get; internal set; }
        public SeriesResult Series { get; internal set; }
        public long Id { get; internal set; }
        
        public CalendarEntry() : base() { }

        public static explicit operator CalendarEntry(JObject job) =>
            FromJObject<CalendarEntry>(job);
    }
}
