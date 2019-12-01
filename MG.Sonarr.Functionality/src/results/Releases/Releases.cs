using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class that defines a response from the "/release" endpoint.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Release : BaseResult
    {
        private const string AGE = "ageMinutes";

        [JsonExtensionData]
        private IDictionary<string, JToken> _data;

        [JsonProperty("absoluteEpisodeNumbers")]
        public int[] AbsoluteEpisodeNumbers { get; set; }

        [JsonIgnore]
        public TimeSpan Age { get; set; }

        [JsonProperty("downloadAllowed")]
        public bool Allowed { get; set; }

        [JsonProperty("approved")]
        public bool Approved { get; set; }

        [JsonProperty("downloadUrl")]
        public Uri DownloadUrl { get; set; }

        [JsonProperty("episodeNumbers")]
        public int[] EpisodeNumbers { get; set; }
        
        [JsonProperty("indexer")]
        public string Indexer { get; set; }

        [JsonProperty("infoUrl")]
        public Uri InfoUrl { get; set; }

        [JsonProperty("indexerId")]
        public int IndexerId { get; set; }

        [JsonProperty("isAbsoluteNumbering")]
        public bool IsAbsoluteNumbering { get; set; }

        [JsonProperty("isDaily")]
        public bool IsDaily { get; set; }

        [JsonProperty("fullSeason")]
        public bool IsFullSeason { get; set; }

        [JsonProperty("isPossibleSpecialEpisode")]
        public bool IsPossibleSpecialEpisode { get; set; }

        [JsonProperty("rejected")]
        public bool IsRejected { get; set; }

        [JsonProperty("special")]
        public bool IsSpecial { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("protocol")]
        public string Protocol { get; set; }

        [JsonProperty("publishDate")]
        public DateTime PublishDate { get; set; }

        [JsonProperty("quality")]
        public ItemQuality Quality { get; set; }

        [JsonProperty("rejections")]
        public string[] Rejections { get; set; }

        [JsonProperty("releaseGroup")]
        public string ReleaseGroup { get; set; }

        [JsonProperty("releaseHash")]
        public string ReleaseHash { get; set; }

        [JsonProperty("releaseWeight")]
        public int ReleaseWeight { get; set; }

        [JsonProperty("guid")]
        public Uri ReleaseUrl { get; set; }

        [JsonProperty("seasonNumber")]
        public int SeasonNumber { get; set; }

        [JsonProperty("seriesTitle")]
        public string SeriesTitle { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("temporarilyRejected")]
        public bool TemporarilyRejected { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("tvDBId")]
        public string TVDBId { get; set; }

        [JsonProperty("tvRageId")]
        public string TVRageId { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (_data.ContainsKey(AGE))
            {
                JToken tokAge = _data[AGE];
                if (tokAge != null)
                {
                    double ageMin = tokAge.ToObject<double>();
                    this.Age = TimeSpan.FromMinutes(ageMin);
                }
            }
        }
    }
}
