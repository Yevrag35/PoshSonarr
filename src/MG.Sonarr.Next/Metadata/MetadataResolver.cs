﻿using MG.Sonarr.Next.Extensions.PSO;
using System.Collections;
using System.Management.Automation;

namespace MG.Sonarr.Next.Metadata
{
    /// <summary>
    /// A dictionary implementation of <see cref="MetadataTag"/> instances that describe the various
    /// types of deserialized API response objects.
    /// </summary>
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

        public bool Add(string tag, string baseUrl, bool supportsId, string[]? canPipeTo = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(tag);
            ArgumentException.ThrowIfNullOrEmpty(baseUrl);
            canPipeTo ??= Array.Empty<string>();

            return _dict.TryAdd(tag, new MetadataTag(baseUrl, tag, supportsId, canPipeTo));
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