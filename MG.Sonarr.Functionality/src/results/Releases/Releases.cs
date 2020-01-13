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
        public int[] AbsoluteEpisodeNumbers { get; private set; }

        [JsonIgnore]
        public TimeSpan Age { get; private set; }

        [JsonProperty("downloadAllowed")]
        public bool Allowed { get; private set; }

        [JsonProperty("approved")]
        public bool Approved { get; private set; }

        [JsonProperty("downloadUrl")]
        public Uri DownloadUrl { get; private set; }

        [JsonProperty("episodeNumbers")]
        public int[] EpisodeNumbers { get; private set; }
        
        [JsonProperty("indexer")]
        public string Indexer { get; private set; }

        [JsonProperty("infoUrl")]
        public Uri InfoUrl { get; private set; }

        [JsonProperty("indexerId")]
        public int IndexerId { get; private set; }

        [JsonProperty("isAbsoluteNumbering")]
        public bool IsAbsoluteNumbering { get; private set; }

        [JsonProperty("isDaily")]
        public bool IsDaily { get; private set; }

        [JsonProperty("fullSeason")]
        public bool IsFullSeason { get; private set; }

        [JsonProperty("isPossibleSpecialEpisode")]
        public bool IsPossibleSpecialEpisode { get; private set; }

        [JsonProperty("rejected")]
        public bool IsRejected { get; private set; }

        [JsonProperty("special")]
        public bool IsSpecial { get; private set; }

        [JsonProperty("language")]
        public string Language { get; private set; }

        [JsonProperty("protocol")]
        public string Protocol { get; private set; }

        [JsonProperty("publishDate")]
        public DateTime PublishDate { get; private set; }

        [JsonProperty("quality")]
        public ItemQuality Quality { get; private set; }

        [JsonProperty("rejections")]
        public string[] Rejections { get; private set; }

        [JsonProperty("releaseGroup")]
        public string ReleaseGroup { get; private set; }

        [JsonProperty("releaseHash")]
        public string ReleaseHash { get; private set; }

        [JsonProperty("releaseWeight")]
        public int ReleaseWeight { get; private set; }

        [JsonProperty("guid")]
        public Uri ReleaseUrl { get; private set; }

        [JsonProperty("seasonNumber")]
        public int SeasonNumber { get; private set; }

        [JsonProperty("seriesTitle")]
        public string SeriesTitle { get; private set; }

        [JsonProperty("size")]
        public long Size { get; private set; }

        [JsonProperty("temporarilyRejected")]
        public bool TemporarilyRejected { get; private set; }

        [JsonProperty("title")]
        public string Title { get; private set; }

        [JsonProperty("tvDBId")]
        public string TVDBId { get; private set; }

        [JsonProperty("tvRageId")]
        public string TVRageId { get; private set; }

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
