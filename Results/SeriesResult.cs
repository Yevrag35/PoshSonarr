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

        public string Title => _title;
        public long SeasonCount => _seasonCount;
        public long EpisodeCount => _episodeCount;
        public long EpisodeFileCount => _episodeFileCount;
        public string Status => _status;
        public string Overview => _overview;
        public DateTime NextAiring => _nextAiring;
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
        public DateTime FirstAired => _firstAired;
        public DateTime LastInfoSync => _lastInfoSync;
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
            var dict = JsonConvert.DeserializeObject<Dictionary<object, object>>(JsonConvert.SerializeObject(job));
            return new SeriesResult(dict);
        }
        #endregion

        #region Private Methods
        private protected SonarrSeriesImage[] ParseImages(JArray jar)
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

        private protected SonarrSeason[] ParseSeasons(JArray jar)
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

        #endregion
    }
}
