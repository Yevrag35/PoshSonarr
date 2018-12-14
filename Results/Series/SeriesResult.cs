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

        internal override string[] SkipThese => new string[2]
        {
            "Images",
            "Seasons"
        };

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

        #region Constructors

        public SeriesResult() : base() { }

        #endregion

        #region Operators

        public static explicit operator SeriesResult(JObject job) =>
            FromJObject<SeriesResult>(job);

        #endregion

        #region Private Methods
        //private SonarrSeriesImage[] ParseImages(JArray jar)
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

        #endregion
    }
}
