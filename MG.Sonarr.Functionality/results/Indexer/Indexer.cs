using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Indexer : IndexerBase, IComparable<Indexer>, IIndexer
    {
        IEnumerable<IField> IIndexer.Fields => base.Fields;

        [JsonProperty("id")]
        public int Id { get; private set; }

        public int CompareTo(Indexer other) => this.Id.CompareTo(other.Id);

        public static Indexer Rename(string newName, Indexer indexer)
        {
            return new Indexer
            {
                ConfigContract = indexer.ConfigContract,
                Fields = indexer.Fields,
                Id = indexer.Id,
                Implementation = indexer.Implementation,
                ImplementationName = indexer.ImplementationName,
                InfoLink = indexer.InfoLink,
                Protocol = indexer.Protocol,
                RssEnabled = indexer.RssEnabled,
                RssSupported = indexer.RssSupported,
                SearchEnabled = indexer.SearchEnabled,
                SearchSupported = indexer.SearchSupported,
                Name = newName
            };
        }
    }
}
