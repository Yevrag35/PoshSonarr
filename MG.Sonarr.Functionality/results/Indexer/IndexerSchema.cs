using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class IndexerSchema : IndexerBase, IIndexerSchema
    {
        [JsonProperty("name")]
        private string _name;

        //[JsonProperty("presets")]
        //private List<Indexer> _presets;

        [JsonIgnore]
        IEnumerable<IField> IIndexer.Fields => base.Fields;

        [JsonIgnore]
        public override string Name
        {
            get => !string.IsNullOrEmpty(_name) ? _name : this.Implementation;
            protected set => _name = value;
        }

        [JsonProperty("presets")]
        public PresetIndexerCollection Presets { get; private set; }

        [JsonIgnore]
        IReadOnlyList<IIndexer> IIndexerSchema.Presets => this.Presets;

        //[OnDeserialized]
        //private void OnDeserialized(StreamingContext context)
        //{
        //    if (_presets != null && _presets.Count > 0)
        //    {
        //        this.Presets = new PresetIndexerCollection(_presets);
        //    }
        //}
    }
}
