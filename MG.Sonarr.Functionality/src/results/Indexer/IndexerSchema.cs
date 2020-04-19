using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    public sealed class IndexerSchema : IndexerBase
    {
        [JsonProperty("presets")]
        public ReadOnlyCollection<Indexer> Presets { get; private set; }
    }
}
