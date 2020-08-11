using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class CalendarEntry : BaseResult, IComparable<CalendarEntry>, IEquatable<CalendarEntry>
    {
        public const string Calendar_DTFormat = "yyyy-MM-ddTHH:mm:ss";

        [JsonProperty("airDateUtc")]
        [JsonConverter(typeof(OffsetConverter))]
        private DateTimeOffset? _airDateUtcOffset;

        [JsonProperty("series")]
        [JsonConverter(typeof(CalendarSeriesConverter))]
        private Dictionary<string, object> _fullSeries;

        [JsonProperty("absoluteEpisodeNumber")]
        public int? AbsoluteEpisodeNumber { get; private set; }

        [JsonIgnore]
        public DateTimeOffset? AirDate => _airDateUtcOffset.HasValue ? _airDateUtcOffset.Value.LocalDateTime : (DateTimeOffset?)null;

        [JsonIgnore]
        public DayOfWeek? DayOfWeek => this.AirDate.HasValue
            ? this.AirDate.Value.DayOfWeek
            : (DayOfWeek?)null;

        [JsonProperty("id")]
        public long EpisodeId { get; private set; }

        [JsonProperty("episodeFileId")]
        [JsonConverter(typeof(NullableLongConverter))]
        public long? EpisodeFileId { get; private set; }

        [JsonProperty("episodeNumber")]
        public int EpisodeNumber { get; private set; }

        [JsonProperty("hasFile")]
        public bool IsDownloaded { get; private set; }

        [JsonProperty("monitored")]
        public bool IsMonitored { get; private set; }

        [JsonProperty("title")]
        public string Name { get; private set; }

        [JsonProperty("seasonNumber")]
        public int? SeasonNumber { get; private set; }

        [JsonIgnore]
        public string Series => _fullSeries["title"] as string;

        [JsonProperty("seriesId")]
        public int SeriesId { get; private set; }     // For backwards compatibility

        [JsonProperty("unverifiedSceneNumbering")]
        public bool UnverifiedSceneNumbering { get; private set; }

        #region ICOMPARABLE METHODS
        public int CompareTo(CalendarEntry other)
        {
            int dateCompare = _airDateUtcOffset.GetValueOrDefault().CompareTo(other._airDateUtcOffset.GetValueOrDefault());
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
                   _airDateUtcOffset.GetValueOrDefault().Equals(other._airDateUtcOffset.GetValueOrDefault());
        }

        #endregion

        #region OTHER METHODS
         public static IUrlParameter[] GetStartEndParameters(string start, string end)
        {
            return new IUrlParameter[2]
            {
                new UrlParameter("start", start),
                new UrlParameter("end", end)
            };
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
        
        public DateTimeOffset? GetTokyoTime()
        {
            if (_airDateUtcOffset.HasValue && _fullSeries != null && _fullSeries["seriesType"].Equals("anime"))
                return _airDateUtcOffset.Value.ToAnimeTime();

            else
                return null;
        }

        #endregion
    }
}
