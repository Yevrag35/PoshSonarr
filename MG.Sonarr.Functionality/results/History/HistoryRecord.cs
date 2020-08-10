using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using MG.Sonarr.Results;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class HistoryRecord : BaseResult, IComparable<HistoryRecord>, IEquatable<HistoryRecord>, IRecord
    {
        #region PRIVATE JSON FIELDS
        [JsonProperty("data", Order = 9)]
        private HistoryData _data;

        [JsonProperty("quality", Order = 4)]
        [JsonConverter(typeof(SimpleQualityConverter))]
        private (int, string) _quality;

        #endregion

        #region JSON PROPERTIES
        [JsonProperty("qualityCutoffNotMet", Order = 5)]
        public bool CutoffNotMet { get; private set; }

        [JsonProperty("date", Order = 6)]
        [JsonConverter(typeof(UtcOffsetConverter))]
        public DateTimeOffset Date { get; private set; }

        [JsonIgnore]
        public string DownloadClient => _data.DownloadClient;

        [JsonProperty("downloadId", Order = 7)]
        public string DownloadId { get; private set; }

        [JsonIgnore]
        public string DroppedPath => _data.DroppedPath;

        [JsonProperty("episodeId", Order = 1)]
        public long EpisodeId { get; private set; }

        [JsonProperty("eventType", Order = 8)]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public HistoryEventType EventType { get; private set; }

        [JsonProperty("id", Order = 12)]
        public long Id { get; private set; }

        [JsonIgnore]
        public string ImportedPath => _data.ImportedPath;

        [JsonIgnore]
        public string Quality => _quality.Item2;

        [JsonIgnore]
        public int QualityProfileId => _quality.Item1;

        [JsonProperty("seriesId", Order = 2)]
        public int SeriesId { get; private set; }

        [JsonProperty("sourceTitle", Order = 3)]
        public string SourceTitle { get; private set; }

        #endregion

        public int CompareTo(HistoryRecord other) => this.Id.CompareTo(other.Id);
        int IComparable<IRecord>.CompareTo(IRecord other) => this.Id.CompareTo(other.Id);
        public bool Equals(HistoryRecord other) => this.Id.Equals(other.Id);
        bool IEquatable<IRecord>.Equals(IRecord other) => this.Id.Equals(other.Id);
    }
}