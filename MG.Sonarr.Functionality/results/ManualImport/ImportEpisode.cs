﻿using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ImportEpisode : BaseResult, IAdditionalInfo, IEpisode
    {
        [JsonExtensionData]
        private IDictionary<string, JToken> _extData;

        [JsonProperty("absoluteEpisodeNumber")]
        public int? AbsoluteEpisodeNumber { get; private set; }

        [JsonIgnore]
        public DateTimeOffset? AirDate { get; private set; }

        [JsonIgnore]
        DateTime? IEpisode.AirDate => this.AirDate.GetValueOrDefault().LocalDateTime;

        [JsonProperty("episodeNumber")]
        public int? EpisodeNumber { get; private set; }

        [JsonProperty("id")]
        public long Id { get; private set; }

        [JsonProperty("monitored")]
        public bool IsMonitored { get; private set; }

        [JsonProperty("title")]
        public string Name { get; private set; }

        [JsonProperty("seasonNumber")]
        public int? SeasonNumber { get; private set; }

        [JsonProperty("seriesId")]
        public long? SeriesId { get; private set; }

        public IDictionary GetAdditionalInfo() => _extData as IDictionary;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (_extData["airDateUtc"] != null)
            {
                this.AirDate = _extData["airDateUtc"].ToObject<DateTimeOffset>().ToLocalTime();
            }
        }

        public void ApplyDataFromEpisode(IEpisode episode)
        {
            this.AbsoluteEpisodeNumber = episode.AbsoluteEpisodeNumber;
            this.AirDate = episode.AirDate;
            this.EpisodeNumber = episode.EpisodeNumber;
            this.Id = episode.Id;
            this.IsMonitored = episode.IsMonitored;
            this.Name = episode.Name;
            this.SeasonNumber = episode.SeasonNumber;
            this.SeriesId = episode.SeriesId;
        }
    }
}
