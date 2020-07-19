using MG.Sonarr.Functionality.Converters;
using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class IndexerTemplate : BaseResult, IIndexer
    {
        private IReadOnlyList<IIndexer> _presets;
        private bool _rss;
        private bool _search;

        [JsonProperty("configContract")]
        public string ConfigContract { get; }
        [JsonProperty("fields")]
        public FieldCollection Fields { get; }
        IEnumerable<IField> IIndexer.Fields => this.Fields;
        [JsonIgnore]
        public bool HasPresets => _presets != null && _presets.Count > 0;
        [JsonProperty("implementation")]
        public string Implementation { get; }
        [JsonProperty("implementationName")]
        public string ImplementationName { get; }
        [JsonProperty("infoLink")]
        public Uri InfoLink { get; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("protocol")]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public DownloadProtocol Protocol { get; }
        [JsonProperty("rssEnabled")]
        public bool RssEnabled
        {
            get => _rss;
            set
            {
                if (this.RssSupported)
                {
                    _rss = value;
                }
                else
                    throw new InvalidOperationException("RSS is not supported with this indexer.");
            }
        }
        [JsonProperty("rssSupported")]
        public bool RssSupported { get; }
        [JsonProperty("searchEnabled")]
        public bool SearchEnabled
        {
            get => _search;
            set
            {
                if (this.SearchSupported)
                {
                    _search = value;
                }
                else
                    throw new InvalidOperationException("Search is not supported with this indexer.");
            }
        }
        [JsonProperty("searchSupported")]
        public bool SearchSupported { get; }

        public IndexerTemplate(IIndexerSchema schema)
        {
            this.ConfigContract = schema.ConfigContract;
            this.Implementation = schema.Implementation;
            this.ImplementationName = schema.ImplementationName;
            this.InfoLink = schema.InfoLink;
            this.Name = schema.Name;
            this.Protocol = schema.Protocol;
            _rss = schema.RssEnabled;
            this.RssSupported = schema.RssSupported;
            _search = schema.SearchEnabled;
            this.SearchSupported = schema.SearchSupported;

            this.Fields = new FieldCollection(schema.Fields);
            _presets = schema.Presets;
        }
    }
}
