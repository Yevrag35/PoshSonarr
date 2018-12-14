using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sonarr.Api.Components;
using Sonarr.Api.Endpoints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sonarr.Api.Results
{
    public class Episode : SonarrResult
    {
        internal override string[] SkipThese => new string[1] { "Series" };

        #region Public Properties

        public int? AbsoluteEpisodeNumber { get; internal set; }
        public DateTime? AirDate { get; internal set; }
        public DateTime? AirDateUtc { get; internal set; }
        public EpisodeFile EpisodeFile { get; internal set; }
        public int? EpisodeFileId { get; internal set; }
        public int? EpisodeNumber { get; internal set; }
        public bool HasFile { get; internal set; }
        public long Id { get; internal set; }
        public bool Monitored { get; internal set; }
        public int? SceneAbsoluteEpisodeNumber { get; internal set; }
        public int? SceneEpisodeNumber { get; internal set; }
        public int? SceneSeasonNumber { get; internal set; }
        public int? SeasonNumber { get; internal set; }
        public SeriesResult Series => GetSeriesFromId();
        public int SeriesId { get; internal set; }
        public string Title { get; internal set; }
        public bool UnverifiedSceneNumbering { get; internal set; }

        #endregion

        #region Constructors

        public Episode() : base() { }

        #endregion

        #region Methods/Operators

        public static explicit operator Episode(JObject job) =>
            FromJObject<Episode>(job);


        private SeriesResult GetSeriesFromId()
        {
            if (!SonarrServiceContext.SeriesDictionary.ContainsKey(this.SeriesId))
            {
                var ep = new Series(this.SeriesId);
                var series = SonarrServiceContext.TheCaller.SonarrGetAs<SeriesResult>(ep).ToArray()[0];
                SonarrServiceContext.SeriesDictionary.Add(this.SeriesId, series);
            }
            return SonarrServiceContext.SeriesDictionary[this.SeriesId];
        }

        #endregion
    }
}
