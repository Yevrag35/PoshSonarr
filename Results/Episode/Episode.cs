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
        private long? _absoluteEpisodeNumber;
        private DateTime? _airDate;
        private DateTime? _airDateUtc;
        private EpisodeFile _episodeFile;
        private long _episodeFileId;
        private long? _episodeNumber;
        private bool _hasFile;
        private long _id;
        private bool _monitored;
        private long? _sceneAbsoluteEpisodeNumber;
        private long? _sceneEpisodeNumber;
        private long? _sceneSeasonNumber;
        private long? _seasonNumber;
        private SeriesResult _series;
        private long _seriesId;
        private string _title;
        private bool _unverifiedSceneNumbering;

        #endregion

        #region Public Properties

        public long? AbsoluteEpisodeNumber => _absoluteEpisodeNumber;
        public DateTime? AirDate => _airDate;
        public DateTime? AirDateUtc => _airDateUtc;
        public EpisodeFile EpisodeFile => _episodeFile;
        public long EpisodeFileId => _episodeFileId;
        public long? EpisodeNumber => _episodeNumber;
        public bool HasFile => _hasFile;
        public long Id => _id;
        public bool Monitored => _monitored;
        public long? SceneAbsoluteEpisodeNumber => _sceneAbsoluteEpisodeNumber;
        public long? SceneEpisodeNumber => _sceneEpisodeNumber;
        public long? SceneSeasonNumber => _sceneSeasonNumber;
        public long? SeasonNumber => _seasonNumber;
        public SeriesResult Series => _series;
        public long SeriesId => _seriesId;
        public string Title => _title;
        public bool UnverifiedSceneNumbering => _unverifiedSceneNumbering;

        #endregion

        #region Constructor

        private Episode(IDictionary dict) => MatchResultsToProperties(dict);

        #endregion

        #region Methods/Operators

        public static explicit operator Episode(JObject job)
        {
            if (job != null)
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<object, object>>(JsonConvert.SerializeObject(job));
                return new Episode(dict);
            }
            else
            {
                return null;
            }
        }
        public static explicit operator Episode(Dictionary<object, object> dict) =>
            dict != null ? new Episode(dict) : null;

        #endregion
    }
}
