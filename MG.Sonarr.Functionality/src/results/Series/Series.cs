using MG.Sonarr.Functionality;
using Newtonsoft.Json;
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
    /// <summary>
    /// The class that defines a response from the "/series" or "/series/lookup" endpoints.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [Serializable]
    public class SeriesResult : BaseResult, IHasTagSet
    {
        private const string AIRTIME = "airTime";
        private const string RATING = "ratings";

        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        [JsonProperty("added")]
        public DateTime? Added { get; private set; }

        [JsonIgnore]
        public string AirTime { get; private set; }

        [JsonProperty("alternateTitles")]
        public AlternateTitle[] AlternateTitles { get; private set; }

        [JsonProperty("cleanTitle")]
        public string CleanTitle { get; private set; }

        [JsonProperty("firstAired")]
        public DateTime? FirstAired { get; private set; }

        [JsonProperty("genres")]
        public string[] Genres { get; private set; }

        [JsonProperty("images")]
        public SeriesImage[] Images { get; private set; }

        [JsonProperty("iMDBId")]
        public string IMDBId { get; private set; }

        [JsonProperty("monitored")]
        public bool IsMonitored { get; set; }

        [Obsolete]
        [JsonIgnore]
        public bool Monitored
        {
            get => this.IsMonitored;
            set
            {
                Console.WriteLine("The property \"Monitored\" is deprecated and will be removed from future releases.  Use \"IsMonitored\" instead.");
                this.IsMonitored = value;
            }
        }

        [JsonProperty("title")]
        public string Name { get; private set; }

        [JsonProperty("network")]
        public string Network { get; private set; }

        [JsonProperty("overview")]
        public string Overview { get; private set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("qualityProfileId")]
        public int QualityProfileId { get; set; }

        [JsonProperty("rating")]
        public float Rating { get; private set; }

        [JsonIgnore]
        public int RatingVotes { get; private set; }

        [JsonProperty("remotePoster")]
        public string RemotePoster { get; private set; }

        [JsonProperty("runtime")]
        public long? Runtime { get; private set; }

        [JsonProperty("seasonCount")]
        public int SeasonCount { get; private set; }

        [JsonProperty("seasons")]
        public SeasonCollection Seasons { get; private set; }

        [JsonProperty("id")]
        public long SeriesId { get; private set; }

        [JsonProperty("seriesType")]
        public SeriesType SeriesType { get; private set; }

        [JsonProperty("sortTitle")]
        public string SortTitle { get; private set; }

        [JsonProperty("status")]
        public SeriesStatusType Status { get; private set; }

        [JsonProperty("tags")]
        public HashSet<int> Tags { get; private set; }

        [JsonProperty("titleSlug")]
        public string TitleSlug { get; private set; }

        [JsonProperty("tvDBId")]
        public long TVDBId { get; private set; }

        [JsonProperty("tvMazeId")]
        public long TVMazeId { get; private set; }

        [JsonProperty("tvRageId")]
        public long TVRageId { get; private set; }

        [JsonProperty("certification")]
        public string TVRating { get; private set; }

        [JsonProperty("useSceneNumbering")]
        public bool UsesSceneNumbering { get; private set; }

        [JsonProperty("seasonFolder")]
        public bool UsingSeasonFolders { get; set; }

        [JsonProperty("year")]
        public int Year { get; private set; }

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
                string tokTime = jtok.ToObject<string>();
                var tokyoTime = DateTime.Parse(tokTime); // In Tokyo Standard Time
                TimeZoneInfo tokyotz = TimeZoneInfo.GetSystemTimeZones().First(x => x.Id.Contains("Tokyo"));
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
