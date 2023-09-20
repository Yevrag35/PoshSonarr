using MG.Sonarr.Next.Services.Extensions;
using System.Collections;
using System.Management.Automation;

namespace MG.Sonarr.Next.Services.Metadata
{
    public sealed class MetadataResolver : IReadOnlyCollection<MetadataTag>
    {
        public const string META_PROPERTY_NAME = "MetadataTag";
        public const char META_PREFIX = '#';
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
            return _dict.TryAdd(tag, new MetadataTag(baseUrl, tag, supportsId));
        }
        public bool AddToObject([ConstantExpected] string tag, [NotNullWhen(true)] object? obj)
        {
            if (!_dict.ContainsKey(tag) || obj is null || obj is not PSObject pso)
            {
                return false;
            }

            return this.AddToObject(tag, pso);
        }
        public bool AddToObject([ConstantExpected] string tag, PSObject pso)
        {
            if (this.TryGetValue(tag, out MetadataTag? meta))
            {
                pso.AddProperty(META_PROPERTY_NAME, meta);
                return true;
            }
            else
            {
                return false;
            }
        }
        public IEnumerator<MetadataTag> GetEnumerator()
        {
            return _dict.Values.GetEnumerator();
        }
        public bool TryGetValue(PSObject pso, [NotNullWhen(true)] out MetadataTag? value)
        {
            if (pso.TryGetProperty(META_PROPERTY_NAME, out string? metaTag)
                &&
                this.TryGetValue(metaTag, out value))
            {
                return true;
            }

            value = null;
            return false;
        }
        public bool TryGetValue(string key, [NotNullWhen(true)] out MetadataTag? value)
        {
            return _dict.TryGetValue(key, out value);
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
