using System.Collections;

namespace MG.Sonarr.Next.Services.Metadata
{
    public sealed class MetadataResolver : IEnumerable<MetadataTag>
    {
        readonly Dictionary<string, MetadataTag> _dict;

        public MetadataTag this[string key] => _dict[key];

        public int Count => _dict.Count;

        public MetadataResolver(int capacity)
        {
            _dict = new(capacity, StringComparer.InvariantCultureIgnoreCase);
        }

        public bool Add(string tag, string baseUrl, bool supportsId)
        {
            ArgumentException.ThrowIfNullOrEmpty(tag);
            ArgumentException.ThrowIfNullOrEmpty(baseUrl);
            return _dict.TryAdd(tag, new MetadataTag { Value = tag, UrlBase = baseUrl, SupportsId = supportsId });
        }
        public IEnumerator<MetadataTag> GetEnumerator()
        {
            return _dict.Values.GetEnumerator();
        }
        public bool TryGetValue(string key, out MetadataTag? value)
        {
            return _dict.TryGetValue(key, out value);
        }

        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
