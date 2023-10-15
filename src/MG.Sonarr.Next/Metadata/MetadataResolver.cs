using MG.Sonarr.Next.Extensions.PSO;
using System.Collections;
using System.Management.Automation;

namespace MG.Sonarr.Next.Metadata
{
    /// <summary>
    /// A dictionary interface of <see cref="MetadataTag"/> instances that describe the various
    /// types of deserialized API response objects.
    /// </summary>
    public interface IMetadataResolver : IReadOnlyCollection<MetadataTag>
    {
        MetadataTag this[string key] { get; }

        bool ContainsKey([NotNullWhen(true)] string? key);
        bool TryGetValue([NotNullWhen(true)] string? key, [NotNullWhen(true)] out MetadataTag? value);
        bool TryGetValue(PSObject pso, [NotNullWhen(true)] out MetadataTag? value);
    }

    /// <summary>
    /// A dictionary implementation of <see cref="MetadataTag"/> instances that describe the various
    /// types of deserialized API response objects.
    /// </summary>
    internal sealed class MetadataResolver : IMetadataResolver
    {
        public static readonly string META_PROPERTY_NAME = "MetadataTag";
        public const char META_PREFIX = '#';
        readonly Dictionary<string, MetadataTag> _dict;

        /// <summary>
        /// Gets the <see cref="MetadataTag"/> associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the <see cref="MetadataTag"/> to get; equal to 
        /// <see cref="MetadataTag.Value"/></param>
        /// <returns>
        ///     The <see cref="MetadataTag"/> associated with the specified key. If the specified key is not
        ///     found, <see cref="MetadataTag.Empty"/> is returned.
        /// </returns>
        public MetadataTag this[string key] => _dict.TryGetValue(key ?? string.Empty, out MetadataTag? tag)
            ? tag
            : MetadataTag.Empty;

        public int Count => _dict.Count;

        public MetadataResolver(int capacity)
        {
            _dict = new(capacity, StringComparer.InvariantCultureIgnoreCase);
        }

        public bool Add(string tag, string baseUrl, bool supportsId)
        {
            return this.Add(tag, baseUrl, supportsId, Array.Empty<string>());
        }
        public bool Add(string tag, string baseUrl, bool supportsId, string[] canPipeTo)
        {
            ArgumentException.ThrowIfNullOrEmpty(tag);
            ArgumentException.ThrowIfNullOrEmpty(baseUrl);
            canPipeTo ??= Array.Empty<string>();

            return _dict.TryAdd(tag, new MetadataTag(baseUrl, tag, supportsId, canPipeTo));
        }

        public bool AddToObject([ConstantExpected] string tag, [NotNullWhen(true)] object? obj)
        {
            ArgumentNullException.ThrowIfNull(tag);

            if (!_dict.ContainsKey(tag) || obj is null || obj is not PSObject pso)
            {
                return false;
            }

            return this.AddToObject(tag, pso);
        }
        public bool AddToObject([ConstantExpected] string tag, PSObject pso)
        {
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(pso);

            bool added = false;
            if (_dict.TryGetValue(tag, out MetadataTag? meta))
            {
                pso.Properties.Add(new MetadataProperty(meta));
                added = true;
            }

            return added;
        }
        public bool ContainsKey([NotNullWhen(true)] string? key)
        {
            return !string.IsNullOrWhiteSpace(key) && _dict.ContainsKey(key);
        }
        public IEnumerator<MetadataTag> GetEnumerator()
        {
            return _dict.Values.GetEnumerator();
        }
        public bool TryGetValue(PSObject pso, [NotNullWhen(true)] out MetadataTag? value)
        {
            ArgumentNullException.ThrowIfNull(pso);

            if (pso.TryGetProperty(META_PROPERTY_NAME, out string? metaTag)
                &&
                this.TryGetValue(metaTag, out value))
            {
                return true;
            }

            value = null;
            return false;
        }
        public bool TryGetValue([NotNullWhen(true)] string? key, [NotNullWhen(true)] out MetadataTag? value)
        {
            value = null;
            return !string.IsNullOrWhiteSpace(key) && _dict.TryGetValue(key, out value);
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
