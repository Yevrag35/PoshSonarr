﻿using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    public class Indexer : IndexerBase, IComparable<Indexer>
    {
        [JsonProperty("id")]
        public int Id { get; private set; }

        public int CompareTo(Indexer other) => this.Id.CompareTo(other.Id);
    }
}
