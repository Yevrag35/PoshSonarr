using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class CalendarEntry : BaseResult, IAdditionalInfo
    {
        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        [JsonProperty("absoluteEpisodeNumber")]
        public int? AbsoluteEpisodeNumber { get; private set; }

        public DateTime? AirDate { get; private set; }

        public DayOfWeek? DayOfWeek => this.AirDate.HasValue
            ? this.AirDate.Value.DayOfWeek
            : (DayOfWeek?)null;

        [JsonProperty("id")]
        public long EpisodeId { get; private set; }

        [JsonProperty("episodeNumber")]
        public int EpisodeNumber { get; private set; }

        // For backwards compatibility
        [Obsolete]
        public bool HasFile => this.IsDownloaded;

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

        public string Series { get; private set; }

        [JsonProperty("seriesId")]
        public int SeriesId { get; private set; }

        [JsonProperty("unverifiedSceneNumbering")]
        public bool UnverifiedSceneNumbering { get; private set; }

        public IDictionary GetAdditionalInfo()
        {
            var ht = new Hashtable();
            if (this.TryGetValue("airDate", out string ad))
            {
                ht.Add("AirDate", ad);
            }
            if (this.TryGetValue("airDateUtc", out string adutc))
            {
                ht.Add("AirDateUtc", adutc);
            }
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

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            JToken adutc = _additionalData["airDateUtc"];
            if (adutc != null)
            {
                this.AirDate = adutc.ToObject<DateTime>().ToLocalTime();
            }

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
    }
}
