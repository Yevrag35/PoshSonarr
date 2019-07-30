﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [Serializable]
    public class SeriesResult : BaseResult
    {
        private const string AIRTIME = "airTime";
        private const string RATING = "ratings";
        //private static readonly TimeZoneInfo TOKYO_TZ = 
        //    TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        public DateTime? Added { get; set; }
        public string AirTime { get; private set; }
        public AlternateTitle[] AlternateTitles { get; set; }
        public string CleanTitle { get; set; }
        public DateTime? FirstAired { get; set; }
        public string[] Genres { get; set; }
        public SeriesImage[] Images { get; set; }
        public string IMDBId { get; set; }
        public bool Monitored { get; set; }
        [JsonProperty("title")]
        public string Name { get; set; }
        public string Network { get; set; }
        public string Overview { get; set; }
        public string Path { get; set; }
        public int QualityProfileId { get; set; }
        public float Rating { get; private set; }
        public int RatingVotes { get; private set; }
        public string RemotePoster { get; set; }
        public long? Runtime { get; set; }
        public int SeasonCount { get; set; }
        public SeasonCollection Seasons { get; set; }
        [JsonProperty("id")]
        public long SeriesId { get; set; }
        public SeriesType SeriesType { get; set; }
        public string SortTitle { get; set; }
        public SeriesStatusType Status { get; set; }
        public HashSet<int> Tags { get; set; }
        public string TitleSlug { get; set; }
        public long TVDBId { get; set; }
        public long TVMazeId { get; set; }
        public long TVRageId { get; set; }
        [JsonProperty("certification")]
        public string TVRating { get; set; }
        public bool UseSceneNumbering { get; set; }
        [JsonProperty("seasonFolder")]
        public bool UsingSeasonFolders { get; set; }
        public int Year { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (this.Status != SeriesStatusType.Ended && _additionalData.ContainsKey(AIRTIME))
            {
                this.AirTime = this.SeriesType == SeriesType.Anime
                    ? this.ConvertFromTokyoTime(_additionalData[AIRTIME])
                    : _additionalData[AIRTIME].ToObject<string>();
            }

            JToken rating = _additionalData[RATING];
            JToken rat = rating.SelectToken("$.value");
            if (rat != null)
                this.Rating = rat.ToObject<float>();

            JToken votes = rating.SelectToken("$.votes");
            if (votes != null)
            {
                this.RatingVotes = votes.ToObject<int>();
            }
        }

        private string ConvertFromTokyoTime(JToken jtok)
        {
            string strRes = null;
            if (jtok != null)
            {
                var tokTime = jtok.ToObject<string>();
                var tokyoTime = DateTime.Parse(tokTime); // In Tokyo Standard Time
                var tokyotz = TimeZoneInfo.GetSystemTimeZones().First(x => x.Id.Contains("Tokyo"));
                if (tokyotz == null)
                    strRes = tokTime;

                else
                {
                    DateTime localTime = TimeZoneInfo.ConvertTime(tokyoTime, tokyotz, TimeZoneInfo.Local);
                    strRes = localTime.ToShortTimeString();
                }
            }

            return strRes;
        }
    }
}
