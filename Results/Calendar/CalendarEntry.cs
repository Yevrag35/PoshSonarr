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
#pragma warning disable IDE0044 // Add readonly modifier
        private long _seriesId;
        private long _episodeFileId;
        private long _seasonNumber;
        private long _episodeNumber;
        private string _title;
        private DateTime _airDateUtc;
        private JObject _episodeFile;
        private bool _hasFile;
        private bool _isMonitored;
        private long _absoluteEpisodeNumber;
        private bool _unverifiedSceneNumbering;
        private JObject _series;
        private long _id;
#pragma warning restore IDE0044 // Add readonly modifier

        public long SeriesId => _seriesId;
        public long EpisodeFileId => _episodeFileId;
        public long SeasonNumber => _seasonNumber;
        public long EpisodeNumber => _episodeNumber;
        public string Title => _title;
        public DateTime AirDateUtc => _airDateUtc;
        public EpisodeFile EpisodeFile => (EpisodeFile)_episodeFile;
        public bool HasFile => _hasFile;
        public bool IsMonitored => _isMonitored;
        public long AbsoluteEpisodeNumber => _absoluteEpisodeNumber;
        public bool UnverifiedSceneNumbering => _unverifiedSceneNumbering;
        public SeriesResult Series => (SeriesResult)_series;
        public long Id => _id;

        private CalendarEntry(IDictionary dict) => MatchResultsToProperties(dict);

        public static explicit operator CalendarEntry(JObject job)
        {
            if (job != null)
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<object, object>>(JsonConvert.SerializeObject(job));
                return new CalendarEntry(dict);
            }
            else
            {
                return null;
            }
        }
        public static explicit operator CalendarEntry(Dictionary<object, object> dict) =>
            new CalendarEntry(dict);
        public static explicit operator CalendarEntry(Dictionary<string, object> dict) =>
            new CalendarEntry(dict);
    }
}
