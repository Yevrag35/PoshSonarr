using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class CalendarEntry : BaseResult, IAdditionalInfo, IComparable<CalendarEntry>, IEquatable<CalendarEntry>
    {
        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        [JsonProperty("absoluteEpisodeNumber")]
        public int? AbsoluteEpisodeNumber { get; private set; }

        [JsonProperty("airDateUtc")]
        public DateTime? AirDateUtc { get; private set; }

        [JsonIgnore]
        public DateTime? AirDate => this.AirDateUtc.HasValue ? (DateTime?)this.AirDateUtc.Value.ToLocalTime() : null;

        [JsonIgnore]
        public DayOfWeek? DayOfWeek => this.AirDateUtc.HasValue
            ? this.AirDateUtc.Value.DayOfWeek
            : (DayOfWeek?)null;

        [JsonProperty("id")]
        public long EpisodeId { get; private set; }

        [JsonProperty("episodeNumber")]
        public int EpisodeNumber { get; private set; }

        [Obsolete]
        [JsonIgnore]
        public bool HasFile => this.IsDownloaded;   // For backwards compatibility

        [JsonProperty("hasFile")]
        public bool IsDownloaded { get; private set; }

        [JsonProperty("monitored")]
        public bool IsMonitored { get; private set; }

        [JsonProperty("title")]
        public string Name { get; private set; }

        [JsonProperty("sceneAbsoluteEpisodeNumber")]
        public int SceneAbsoluteEpisodeNumber { get; private set; }

        [JsonProperty("sceneEpisodeNumber")]
        public int SceneEpisodeNumber { get; private set; }

        [JsonProperty("sceneSeasonNumber")]
        public int SceneSeasonNumber { get; private set; }

        [JsonIgnore]
        public string Series { get; private set; }

        [JsonProperty("seriesId")]
        public int SeriesId { get; private set; }     // For backwards compatibility

        [JsonProperty("unverifiedSceneNumbering")]
        public bool UnverifiedSceneNumbering { get; private set; }

        #region ICOMPARABLE METHODS
        public int CompareTo(CalendarEntry other)
        {
            int dateCompare = this.AirDateUtc.GetValueOrDefault().CompareTo(other.AirDateUtc.GetValueOrDefault());
            if (dateCompare != 0)
                return dateCompare;
            
            else
            {
                int seriesCompare = this.Series.CompareTo(other.Series);
                if (seriesCompare != 0)
                    return seriesCompare;

                else
                    return this.EpisodeId.CompareTo(other.EpisodeId);
            }
        }

        #endregion

        #region IEQUATABLE METHODS
        public bool Equals(CalendarEntry other)
        {
            return this.EpisodeId == other.EpisodeId &&
                   this.AirDateUtc.GetValueOrDefault().Equals(other.AirDateUtc.GetValueOrDefault());
        }

        #endregion

        #region OTHER METHODS
        public IDictionary GetAdditionalInfo()
        {
            var ht = new Hashtable();
            //if (this.TryGetValue("airDate", out string ad))
            //{
            //    ht.Add("AirDate", ad);
            //}
            //if (this.TryGetValue("airDateUtc", out string adutc))
            //{
            //    ht.Add("AirDateUtc", adutc);
            //}
            if (this.TryGetValue("episodeFile", out EpisodeResult sonarrEp))
            {
                ht.Add("EpisodeFile", sonarrEp);
            }
            if (this.TryGetValue("episodeFileId", out long id))
            {
                ht.Add("EpisodeFileId", id);
            }
            if (this.TryGetValue("seasonNumber", out long sn))
            {
                ht.Add("SeasonNumber", sn);
            }
            if (this.TryGetValue("series", out SeriesResult series))
            {
                ht.Add("Series", series);
            }
            return ht;
        }
        public SeriesResult GetSeries()
        {
            SeriesResult result = null;
            if (this.TryGetValue("series", out SeriesResult outSer))
            {
                result = outSer;
            }
            return result;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            //JToken adutc = _additionalData["airDateUtc"];
            //if (adutc != null)
            //{
            //    this.AirDate = adutc.ToObject<DateTime>().ToLocalTime();
            //}

            JToken tokSer = _additionalData["series"];
            if (tokSer != null)
            {
                JToken serTit = tokSer.SelectToken("$.title");
                if (serTit != null)
                {
                    this.Series = serTit.ToObject<string>();
                }
            }
        }
        private bool TryGetValue<T>(string key, out T value)
        {
            value = default;
            bool result = false;
            if (_additionalData.ContainsKey(key))
            {
                JToken jtok = _additionalData[key];
                if (jtok != null)
                {
                    try
                    {
                        value = jtok.ToObject<T>();
                        result = true;
                    }
                    catch { }
                }
            }

            return result;
        }

        #endregion
    }
}
