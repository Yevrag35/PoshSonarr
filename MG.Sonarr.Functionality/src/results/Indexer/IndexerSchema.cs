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
    public sealed class IndexerSchema : IndexerBase
    {
        [JsonExtensionData]
        private IDictionary<string, JToken> _extData;

        [JsonProperty("name")]
        private string _name;

        [JsonIgnore]
        public override string Name
        {
            get => !string.IsNullOrEmpty(_name) ? _name : this.Implementation;
            protected set => _name = value;
        }

        [JsonProperty("presets")]
        public ReadOnlyCollection<Indexer> Presets { get; private set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (string.IsNullOrEmpty(base.Name))
            {
                base.Name = base.Implementation;
            }
        }
    }
}
