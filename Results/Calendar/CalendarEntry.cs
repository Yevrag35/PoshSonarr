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
        //#pragma warning disable IDE0044 // Add readonly modifier
        //        private long _seriesId;
        //        private long _episodeFileId;
        //        private long _seasonNumber;
        //        private long _episodeNumber;
        //        private string _title;
        //        private DateTime? _airDateUtc;
        //        private JObject _episodeFile;
        //        private bool _hasFile;
        //        private bool _isMonitored;
        //        private long _absoluteEpisodeNumber;
        //        private bool _unverifiedSceneNumbering;
        //        private JObject _series;
        //        private long _id;
        //#pragma warning restore IDE0044 // Add readonly modifier

        public long SeriesId { get; internal set; }
        public long EpisodeFileId { get; internal set; }
        public long SeasonNumber { get; internal set; }
        public long EpisodeNumber { get; internal set; }
        public string Title { get; internal set; }
        public DateTime? AirDate { get; internal set; }
        public DateTime? AirDateUtc { get; internal set; }
        public EpisodeFile EpisodeFile { get; internal set; }
        public bool HasFile { get; internal set; }
        public bool IsMonitored { get; internal set; }
        public long AbsoluteEpisodeNumber { get; internal set; }
        public bool UnverifiedSceneNumbering { get; internal set; }
        public SeriesResult Series { get; internal set; }
        public long Id { get; internal set; }

        //private CalendarEntry(IDictionary dict) => MatchResultsToProperties(dict);

        //public static explicit operator CalendarEntry(JObject job)
        //{
        //    if (job != null)
        //    {
        //        var dict = JsonConvert.DeserializeObject<Dictionary<object, object>>(JsonConvert.SerializeObject(job));
        //        return new CalendarEntry(dict);
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}
        //public static explicit operator CalendarEntry(Dictionary<object, object> dict) =>
        //    new CalendarEntry(dict);
        //public static explicit operator CalendarEntry(Dictionary<string, object> dict) =>
        //    new CalendarEntry(dict);
        
    }
}
