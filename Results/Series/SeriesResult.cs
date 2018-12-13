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

        //#pragma warning disable IDE0044 // Add readonly modifier
        //        private string _title;
        //        private long _seasonCount;
        //        private long _episodeCount;
        //        private long _episodeFileCount;
        //        private string _status;
        //        private string _overview;
        //        private DateTime? _nextAiring;
        //        private string _network;
        //        private string _airTime;
        //        private JArray _images;
        //        private JArray _seasons;
        //        private long _year;
        //        private string _path;
        //        private long _qualityProfileId;
        //        private bool _seasonFolder;
        //        private bool _monitored;
        //        private bool _useSceneNumbering;
        //        private long _runtime;
        //        private long _tVDBId;
        //        private long _tVRageId;
        //        private DateTime? _firstAired;
        //        private DateTime? _lastInfoSync;
        //        private string _seriesType;
        //        private string _cleanTitle;
        //        private string _iMDBId;
        //        private string _titleSlug;
        //        private long _id;
        //#pragma warning restore IDE0044 // Add readonly modifier

        public string Title { get; internal set; }
        public long SeasonCount { get; internal set; }
        public long EpisodeCount { get; internal set; }
        public long EpisodeFileCount { get; internal set; }
        public string Status { get; internal set; }
        public string Overview { get; internal set; }
        public DateTime? NextAiring { get; internal set; }
        public DateTime? NextAiringUtc { get; internal set; }
        public string Network { get; internal set; }
        public string AirTime { get; internal set; }
        public long Year { get; internal set; }
        public string Path { get; internal set; }
        public long QualityProfileId { get; internal set; }
        public bool SeasonFolder { get; internal set; }
        public bool Monitored { get; internal set; }
        public bool UseSceneNumbering { get; internal set; }
        public long Runtime { get; internal set; }
        public long TVDBId { get; internal set; }
        public long TVRageId { get; internal set; }
        public DateTime? FirstAired { get; internal set; }
        public DateTime? FirstAiredUtc { get; internal set; }
        public DateTime? LastInfoSync { get; internal set; }
        public DateTime? LastInfoSyncUtc { get; internal set; }
        public string SeriesType { get; internal set; }
        public string CleanTitle { get; internal set; }
        public string IMDBId { get; internal set; }
        public string TitleSlug { get; internal set; }
        public long Id { get; internal set; }


        //public SonarrSeriesImage[] Images
        //{
        //    get
        //    {
        //        if (_imgList == null)
        //        {
        //            _imgList = ParseImages(_images);
        //        }
        //        return _imgList;
        //    }
        //}
        //public SonarrSeason[] Seasons
        //{
        //    get
        //    {
        //        if (_seasonList == null)
        //        {
        //            _seasonList = ParseSeasons(_seasons);
        //        }
        //        return _seasonList;
        //    }
        //}

        #endregion

        //#region Constructors
        //public SeriesResult(IDictionary series) => MatchResultsToProperties(series);

        //public static explicit operator SeriesResult(JObject job)
        //{
        //    if (job != null)
        //    {
        //        var dict = JsonConvert.DeserializeObject<Dictionary<object, object>>(JsonConvert.SerializeObject(job));
        //        return new SeriesResult(dict);
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        //public static explicit operator SeriesResult(Dictionary<object, object> dict) =>
        //    new SeriesResult(dict);
        //public static explicit operator SeriesResult(Dictionary<string, object> dict) =>
        //    new SeriesResult(dict);
        //#endregion

        //#region Private Methods
        //private protected SonarrSeriesImage[] ParseImages(JArray jar)
        //{
        //    if (jar != null)
        //    {
        //        var list = new List<SonarrSeriesImage>();
        //        for (int i = 0; i < jar.Count; i++)
        //        {
        //            var job = (JObject)jar[i];
        //            var ssi = new SonarrSeriesImage(job);
        //            list.Add(ssi);
        //        }
        //        return list.ToArray();
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        //private protected SonarrSeason[] ParseSeasons(JArray jar)
        //{
        //    if (jar != null)
        //    {
        //        var list = new List<SonarrSeason>();
        //        for (int i = 0; i < jar.Count; i++)
        //        {
        //            var job = (JObject)jar[i];
        //            var season = new SonarrSeason(job);
        //            list.Add(season);
        //        }
        //        return list.ToArray();
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        //#endregion
    }
}
