using MG.Api.Rest;
using MG.Api.Rest.Generic;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace MG.Sonarr.Functionality
{
    public class IndexerSchemaCollection : IReadOnlyList<IndexerSchema>
    {
        //private List<IndexerSchema> _list;
        private SortedList<string, IndexerSchema> _list;

        public IndexerSchema this[int index] => _list.Values[index];
        public IndexerSchema this[string schemaName] => _list[schemaName];

        public int Count => _list.Count;
        public IList<string> Names => _list.Keys;

        private IndexerSchemaCollection(IEnumerable<IndexerSchema> schemas)
        {
            _list = new SortedList<string, IndexerSchema>(12, SonarrFactory.NewIgnoreCaseComparer());
            foreach (IndexerSchema schema in schemas)
            {
                _list.Add(schema.Name, schema);
            }
        }

        public IEnumerator<IndexerSchema> GetEnumerator() => _list.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.Values.GetEnumerator();

        public static IndexerSchemaCollection FromSchemas(IEnumerable<IndexerSchema> schemas) => new IndexerSchemaCollection(schemas);
    }
}
