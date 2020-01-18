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
    public class SeriesResult : SearchSeries
    {
        private const string AIRTIME = "airTime";
        private const string RATING = "ratings";

        //[JsonExtensionData]
        //private IDictionary<string, JToken> _additionalData;

        [JsonProperty("alternateTitles")]
        public AlternateTitle[] AlternateTitles { get; private set; }

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

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("qualityProfileId")]
        public int QualityProfileId { get; set; }

        [JsonProperty("remotePoster")]
        public string RemotePoster { get; private set; }

        [JsonProperty("seasons")]
        public SeasonCollection Seasons { get; private set; }

        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonProperty("tags")]
        public HashSet<int> Tags { get; private set; }

        [JsonProperty("certification")]
        public string TVRating { get; private set; }

        [JsonProperty("seasonFolder")]
        public bool UsingSeasonFolders { get; set; }

        //[OnDeserialized]
        //private void OnDeserialized(StreamingContext context)
        //{
        //    if (this.Status != SeriesStatusType.Ended && _additionalData.ContainsKey(AIRTIME))
        //    {
        //        this.AirTime = this.SeriesType == SeriesType.Anime
        //            ? this.ConvertFromTokyoTime(_additionalData[AIRTIME])
        //            : _additionalData[AIRTIME].ToObject<string>();
        //    }
        //}

        //private string ConvertFromTokyoTime(JToken jtok)
        //{
        //    string strRes = null;
        //    if (jtok != null)
        //    {
        //        string tokTime = jtok.ToObject<string>();
        //        var tokyoTime = DateTime.Parse(tokTime); // In Tokyo Standard Time
        //        TimeZoneInfo tokyotz = TimeZoneInfo.GetSystemTimeZones().First(x => x.Id.Contains("Tokyo"));
        //        if (tokyotz == null)
        //            strRes = tokTime;

        //        else
        //        {
        //            DateTime localTime = TimeZoneInfo.ConvertTime(tokyoTime, tokyotz, TimeZoneInfo.Local);
        //            strRes = localTime.ToShortTimeString();
        //        }
        //    }

        //    return strRes;
        //}
    }
}
