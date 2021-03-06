﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MediaInfo : BaseResult
    {
        [JsonProperty("audioChannels")]
        public double AudioChannels { get; set; }

        [JsonProperty("audioCodec")]
        public string AudioCodec { get; set; }

        [JsonProperty("videoCodec")]
        public string VideoCodec { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class QualityDetails : BaseResult, IComparable<QualityDetails>
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("resolution")]
        public int Resolution { get; set; }

        public int CompareTo(QualityDetails other) => this.Id.CompareTo(other.Id);

        public static implicit operator QualityDetails(Quality quality)
        {
            return new QualityDetails
            {
                Id = quality.Id,
                Name = quality.Name,
                Resolution = quality.Resolution,
                Source = quality.Source.ToString()
            };
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Ratings : BaseResult
    {
        [JsonProperty("value")]
        public float Value { get; private set; }

        [JsonProperty("votes")]
        public long Votes { get; private set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Revision : BaseResult
    {
        [JsonProperty("real")]
        public int Real { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; } = 1;
    }
}
