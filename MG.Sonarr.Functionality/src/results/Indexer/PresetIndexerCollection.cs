using MG.Sonarr.Results;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class PresetIndexerCollection : ResultCollectionBase<Indexer>, IReadOnlyList<Indexer>, IReadOnlyList<IIndexer>
    {
        public Indexer this[int index] => base.InnerList[index];
        IIndexer IReadOnlyList<IIndexer>.this[int index] => this[index];

        [JsonConstructor]
        internal PresetIndexerCollection(IEnumerable<Indexer> indexers) : base(indexers) { }

        IEnumerator<IIndexer> IEnumerable<IIndexer>.GetEnumerator()
        {
            foreach (IIndexer indexer in base.InnerList)
            {
                yield return indexer;
            }
        }
    }
}
