using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class Episode : SonarrResult
    {
        #region Private Properties
        //private long? _absoluteEpisodeNumber;
        ////private string _airDate;
        //private DateTime? _airDateUtc;
        //private EpisodeFile _episodeFile;
        ////private long? _episodeFileId;
        //private long? _episodeNumber;
        //private bool _hasFile;
        //private long _id;
        //private bool _monitored;
        //private long? _sceneAbsoluteEpisodeNumber;
        //private long? _sceneEpisodeNumber;
        //private long? _sceneSeasonNumber;
        //private long? _seasonNumber;
        ////private SeriesResult _series;
        //private long _seriesId;
        //private string _title;
        //private bool _unverifiedSceneNumbering;

        #endregion

        #region Public Properties

        public long? AbsoluteEpisodeNumber { get; internal set; }
        public DateTime? AirDate { get; internal set; }
        public DateTime? AirDateUtc { get; internal set; }
        public EpisodeFile EpisodeFile { get; internal set; }
        public long? EpisodeFileId { get; internal set; }
        public long? EpisodeNumber { get; internal set; }
        public bool HasFile { get; internal set; }
        public long Id { get; internal set; }
        public bool Monitored { get; internal set; }
        public long? SceneAbsoluteEpisodeNumber { get; internal set; }
        public long? SceneEpisodeNumber { get; internal set; }
        public long? SceneSeasonNumber { get; internal set; }
        public long? SeasonNumber { get; internal set; }
        //public SeriesResult Series { get; internal set; }
        public long SeriesId { get; internal set; }
        public string Title { get; internal set; }
        public bool UnverifiedSceneNumbering { get; internal set; }

        #endregion

        //#region Constructor

        //private Episode(IDictionary dict) => MatchResultsToProperties(dict);

        //#endregion

        //#region Methods/Operators

        //public static explicit operator Episode(JObject job)
        //{
        //    if (job != null)
        //    {
        //        var dict = JsonConvert.DeserializeObject<Dictionary<object, object>>(JsonConvert.SerializeObject(job));
        //        return new Episode(dict);
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}
        //public static explicit operator Episode(Dictionary<object, object> dict) =>
        //    dict != null ? new Episode(dict) : null;

        //#endregion
    }
}
