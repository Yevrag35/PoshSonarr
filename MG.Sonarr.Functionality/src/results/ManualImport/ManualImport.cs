using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// A result model from the /api/manualimport endpoint.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ManualImport : SizedResult
    {
        [JsonExtensionData]
        private IDictionary<string, JToken> _extData;

        [JsonProperty("quality", Order = 5)]
        internal ItemQuality _quality;

        [JsonProperty("series")]
        internal JObject _series;

        #region JSON PROPERTIES
        [JsonProperty("episodes")]
        public ImportEpisode[] Episodes { get; private set; }

        [JsonProperty("path", Order = 1)]
        public string FullPath { get; private set; }

        [JsonProperty("id", Order = 8)]
        public long Id { get; private set; }

        [JsonProperty("name", Order = 3)]
        public string Name { get; private set; }

        [JsonIgnore]
        public string Quality => _quality?.Quality?.Name;

        //[JsonIgnore]  // This turns to be wrong somehow?
        //public int QualityProfileId => _quality.Quality.Id;

        [JsonProperty("qualityWeight", Order = 6)]
        public int QualityWeight { get; private set; }

        [JsonProperty("rejections", Order = 7)]
        public Rejection[] Rejections { get; private set; }

        [JsonProperty("relativePath", Order = 2)]
        public string RelativePath { get; private set; }

        [JsonIgnore]
        public string Series => _series?.SelectToken("$.title").ToObject<string>();

        [JsonProperty("size", Order = 4)]
        public override sealed long SizeOnDisk { get; protected set; }

        #endregion

        public JObject PostThis()
        {
            return new JObject
            {
                new JProperty("episodeIds", this.Episodes.Select(x => x.Id).ToArray()),
                new JProperty("path", this.FullPath),
                new JProperty("quality", JObject.FromObject(_quality)),
                new JProperty("seriesId", _series["id"].ToObject<long>()),
            };
        }
    }
}