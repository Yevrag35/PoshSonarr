using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    public class CalendarEntry : BaseResult
    {
        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        public int? AbsoluteEpisodeNumber { get; set; }
        public DateTime? AirDate { get; private set; }
        public DayOfWeek? DayOfWeek => this.AirDate.HasValue
            ? this.AirDate.Value.DayOfWeek
            : (DayOfWeek?)null;

        [JsonProperty("id")]
        public long EpisodeId { get; set; }
        public int EpisodeNumber { get; set; }
        public bool HasFile { get; set; }
        [JsonProperty("monitored")]
        public bool IsMonitored { get; set; }
        [JsonProperty("title")]
        public string Name { get; set; }
        public int SceneAbsoluteEpisodeNumber { get; set; }
        public int SceneEpisodeNumber { get; set; }
        public int SceneSeasonNumber { get; set; }
        [JsonIgnore]
        public string Series { get; private set; }
        public int SeriesId { get; set; }

        public bool UnverifiedSceneNumbering { get; set; }

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
    }
}
