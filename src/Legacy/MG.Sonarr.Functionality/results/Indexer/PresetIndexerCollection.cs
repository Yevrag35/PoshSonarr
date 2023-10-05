using MG.Sonarr.Results;
using MG.Sonarr.Results.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    public class PresetIndexerCollection : ResultListBase<Indexer>, IReadOnlyList<IIndexer>
    {
        IIndexer IReadOnlyList<IIndexer>.this[int index] => this[index];

        public PresetIndexerCollection() : base() { }
        
        [JsonConstructor]
        internal PresetIndexerCollection(IEnumerable<Indexer> indexers) : base(indexers) { }

        internal void AddRange(IEnumerable<Indexer> indexers) => base.InnerList.AddRange(indexers);
        IEnumerator<IIndexer> IEnumerable<IIndexer>.GetEnumerator()
        {
            foreach (IIndexer indexer in base.InnerList)
            {
                yield return indexer;
            }
        }
    }
}
