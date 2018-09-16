using MG.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sonarr.Api.Results
{
    public class SeriesResult : SonarrResult    // represents 1 series only...
    {
        #region Properties/Fields/Constants
        private SonarrSeriesImage[] _imgList;
        private SonarrSeason[] _seasonList;

#pragma warning disable IDE0044 // Add readonly modifier
        private string _title;
        private long _seasonCount;
        private long _episodeCount;
        private long _episodeFileCount;
        private string _status;
        private string _overview;
        private DateTime _nextAiring;
        private string _network;
        private string _airTime;
        private JArray _images;
        private JArray _seasons;
        private long _year;
        private string _path;
        private long _qualityProfileId;
        private bool _seasonFolder;
        private bool _monitored;
        private bool _useSceneNumbering;
        private long _runtime;
        private long _tVDBId;
        private long _tVRageId;
        private DateTime _firstAired;
        private DateTime _lastInfoSync;
        private string _seriesType;
        private string _cleanTitle;
        private string _iMDBId;
        private string _titleSlug;
        private long _id;
#pragma warning restore IDE0044 // Add readonly modifier

        public string Title => _title;
        public long SeasonCount => _seasonCount;
        public long EpisodeCount => _episodeCount;
        public long EpisodeFileCount => _episodeFileCount;
        public string Status => _status;
        public string Overview => _overview;
        public DateTime NextAiring => _nextAiring.ToLocalTime();
        public DateTime NextAiringUtc => _nextAiring;
        public string Network => _network;
        public string AirTime => _airTime;
        public long Year => _year;
        public string Path => _path;
        public long QualityProfileId => _qualityProfileId;
        public bool SeasonFolder => _seasonFolder;
        public bool Monitored => _monitored;
        public bool UseSceneNumbering => _useSceneNumbering;
        public long Runtime => _runtime;
        public long TVDBId => _tVDBId;
        public long TVRageId => _tVRageId;
        public DateTime FirstAired => _firstAired.ToLocalTime();
        public DateTime FirstAiredUtc => _firstAired;
        public DateTime LastInfoSync => _lastInfoSync.ToLocalTime();
        public DateTime LastInfoSyncUtc => _lastInfoSync;
        public string SeriesType => _seriesType;
        public string CleanTitle => _cleanTitle;
        public string IMDBId => _iMDBId;
        public string TitleSlug => _titleSlug;
        public long Id => _id;


        public SonarrSeriesImage[] Images
        {
            get
            {
                if (_imgList == null)
                {
                    _imgList = ParseImages(_images);
                }
                return _imgList;
            }
        }
        public SonarrSeason[] Seasons
        {
            get
            {
                if (_seasonList == null)
                {
                    _seasonList = ParseSeasons(_seasons);
                }
                return _seasonList;
            }
        }

        #endregion

        #region Constructors
        public SeriesResult(IDictionary series) => MatchResultsToProperties(series);

        public static explicit operator SeriesResult(JObject job)
        {
            if (job != null)
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<object, object>>(JsonConvert.SerializeObject(job));
                return new SeriesResult(dict);
            }
            else
            {
                return null;
            }
        }

        public static explicit operator SeriesResult(Dictionary<object, object> dict) =>
            new SeriesResult(dict);
        public static explicit operator SeriesResult(Dictionary<string, object> dict) =>
            new SeriesResult(dict);
        #endregion

        #region Private Methods
        private protected SonarrSeriesImage[] ParseImages(JArray jar)
        {
            if (jar != null)
            {
                var list = new List<SonarrSeriesImage>();
                for (int i = 0; i < jar.Count; i++)
                {
                    var job = (JObject)jar[i];
                    var ssi = new SonarrSeriesImage(job);
                    list.Add(ssi);
                }
                return list.ToArray();
            }
            else
            {
                return null;
            }
        }

        private protected SonarrSeason[] ParseSeasons(JArray jar)
        {
            if (jar != null)
            {
                var list = new List<SonarrSeason>();
                for (int i = 0; i < jar.Count; i++)
                {
                    var job = (JObject)jar[i];
                    var season = new SonarrSeason(job);
                    list.Add(season);
                }
                return list.ToArray();
            }
            else
            {
                return null;
            }
        }

        #endregion
    }
}
