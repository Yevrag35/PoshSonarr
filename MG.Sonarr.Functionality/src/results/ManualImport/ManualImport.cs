using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// A result model from the /api/manualimport endpoint.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ManualImport : SizedResult, IAdditionalInfo
    {
        [JsonExtensionData]
        private IDictionary<string, JToken> _extData;

        [JsonProperty("quality", Order = 5)]
        internal ItemQuality _quality;

        [JsonProperty("series")]
        private NameId _series;

        #region JSON PROPERTIES
        [JsonProperty("episodes")]
        private List<ImportEpisode> _matchedEpisodes;

        [JsonIgnore]
        public List<IEpisode> Episodes { get; } = new List<IEpisode>();

        [JsonProperty("path", Order = 1)]
        public string FullPath { get; private set; }

        [JsonProperty("id", Order = 8)]
        public long Id { get; private set; }

        [JsonProperty("name", Order = 3)]
        public string Name { get; private set; }

        [JsonIgnore]
        public string Quality => _quality?.Quality?.Name;

        [JsonProperty("qualityWeight", Order = 6)]
        public int QualityWeight { get; private set; }

        [JsonProperty("rejections", Order = 7)]
        public Rejection[] Rejections { get; private set; }

        [JsonProperty("relativePath", Order = 2)]
        public string RelativePath { get; private set; }

        [JsonIgnore]
        public string Series => _series.Name;

        [JsonProperty("size", Order = 4)]
        public override sealed long SizeOnDisk { get; protected set; }

        #endregion

        public void ApplySeries(ISeries series)
        {
            _series.Id = series.Id;
            _series.Name = series.Name;
        }

        public IDictionary GetAdditionalInfo() => _extData as IDictionary;

        public bool IsReadyToPost()
        {
            bool hasSeries = !string.IsNullOrWhiteSpace(_series.Name) && _series.Id > 0;
            if (hasSeries)
            {
                return this.Episodes.TrueForAll(x => x.Id > 0 && Convert.ToInt32(x.SeriesId.GetValueOrDefault()) == _series.Id);
            }
            else
                return false;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (_matchedEpisodes != null && _matchedEpisodes.Count > 0)
            {
                this.Episodes.AddRange(_matchedEpisodes);
            }
            if (_series == null)
            {
                _series = new NameId
                {
                    Name = null,
                    Id = -1
                };
            }
        }
        public JObject PostThis()
        {
            if (!this.IsReadyToPost())
            {
                throw new InvalidOperationException("Cannot post a manual import object without an identified series.");
            }

            return new JObject
            {
                new JProperty("episodeIds", this.Episodes.Select(x => x.Id).ToArray()),
                new JProperty("path", this.FullPath),
                new JProperty("quality", JObject.FromObject(_quality)),
                new JProperty("seriesId", _series.Id)
            };
        }
        public override string ToJson() => JsonConvert.SerializeObject(this.PostThis(), Formatting.Indented);
        public override string ToJson(IDictionary parameters)
        {
            JObject posted = this.PostThis();
            foreach (DictionaryEntry de in parameters)
            {
                posted.Add(new JProperty(Convert.ToString(de.Key), de.Value));
            }

            return JsonConvert.SerializeObject(posted, Formatting.Indented);
        }
    }
}