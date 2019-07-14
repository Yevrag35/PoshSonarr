using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MG.Sonarr.Results
{
    public class SeriesResult : ISonarrResult
    {
        private List<Season> _seasons;

        public DateTime? Added { get; set; }
        public string AirTime { get; set; }
        public string Certification { get; set; }
        public string CleanTitle { get; set; }
        public DateTime? FirstAired { get; set; }
        public string[] Genres { get; set; }
        public long Id { get; set; }
        public SeriesImage[] Images { get; set; }
        public string IMDBId { get; set; }
        public bool Monitored { get; set; }
        public string Network { get; set; }
        public string Overview { get; set; }
        public int ProfileId { get; set; }
        public int QualityProfileId { get; set; }
        public object Ratings { get; set; }
        public string RemotePoster { get; set; }
        public long? Runtime { get; set; }
        public int? SeasonCount { get; set; }
        public bool SeasonFolder { get; set; }
        public IEnumerable<Season> Seasons => _seasons.AsEnumerable();
        public string SeriesType { get; set; }
        public string SortTitle { get; set; }
        public string Status { get; set; }
        public object[] Tags { get; set; }
        public string Title { get; set; }
        public string TitleSlug { get; set; }
        public long TVDBId { get; set; }
        public long? TVMazeId { get; set; }
        public long? TVRageId { get; set; }
        public bool UseSceneNumbering { get; set; }
        public int Year { get; set; }

        internal void AddSeason(Season season)
        {
            if (_seasons == null)
                _seasons = new List<Season>(new Season[1] { season });

            else
                _seasons.Add(season);
        }

        internal void AddSeason(JToken token)
        {
            var season = new Season(token);
            this.AddSeason(season);
        }
    }
}
