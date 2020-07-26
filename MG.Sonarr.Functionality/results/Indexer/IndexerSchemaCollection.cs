using MG.Api.Rest;
using MG.Api.Rest.Generic;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MG.Sonarr.Functionality
{
    public class IndexerSchemaCollection : IReadOnlyList<IndexerSchema>
    {
        private List<IndexerSchema> _list;

        public IndexerSchema this[int index] => _list[index];
        public IndexerSchema this[string schemaName] => _list.Find(x => x.Name.Equals(schemaName, StringComparison.CurrentCultureIgnoreCase));

        public int Count => _list.Count;
        public string[] Names => _list.Select(x => x.Name).ToArray();

        private IndexerSchemaCollection(IList<IndexerSchema> schemas)
        {
            if (schemas == null)
                _list = new List<IndexerSchema>();

            else
                _list = new List<IndexerSchema>(schemas);
        }

        public IEnumerator<IndexerSchema> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        public static IndexerSchemaCollection FromSchemas(IList<IndexerSchema> schemas) => new IndexerSchemaCollection(schemas);
    }
}
