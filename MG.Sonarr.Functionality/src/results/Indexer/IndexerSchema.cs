﻿using MG.Sonarr.Functionality;
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
    public sealed class IndexerSchema : IndexerBase, IIndexSchema
    {
        [JsonProperty("name")]
        private string _name;

        [JsonIgnore]
        IEnumerable<IField> IIndexSchema.Fields => base.Fields;

        [JsonIgnore]
        public override string Name
        {
            get => !string.IsNullOrEmpty(_name) ? _name : this.Implementation;
            protected set => _name = value;
        }

        [JsonProperty("presets")]
        public PresetIndexerCollection Presets { get; private set; }

        [JsonIgnore]
        IReadOnlyList<IIndexer> IIndexSchema.Presets => this.Presets;
    }
}
