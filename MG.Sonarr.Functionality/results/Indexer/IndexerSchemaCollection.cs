using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using MG.Sonarr.Functionality.Extensions;
using MG.Sonarr.Functionality.Collections;

namespace MG.Sonarr.Functionality
{
    public class IndexerSchemaCollection : SortedListBase<IndexerSchema>, IReadOnlyList<IndexerSchema>
    {
        public IndexerSchema this[int index]
        {
            get
            {
                int posIndex = this.GetPositiveIndex(index);
                return posIndex > -1 ? base.InnerList.Values[posIndex] : null;
            }
        }
        public IList<string> Names => base.InnerList.Keys;

        private IndexerSchemaCollection(IEnumerable<IndexerSchema> schemas)
            : base(schemas, x => x.Name)
        {
        }

        public static IndexerSchemaCollection FromSchemas(IEnumerable<IndexerSchema> schemas) => new IndexerSchemaCollection(schemas);
    }
}
