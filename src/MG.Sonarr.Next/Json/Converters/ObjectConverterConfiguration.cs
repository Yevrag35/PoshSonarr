using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Json.Converters.Spans;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Json.Converters
{
    public interface IObjectConverterConfig
    {
        IObjectConverterConfig AddConvertProperties(IEnumerable<KeyValuePair<string, Type>> pairs);
        IObjectConverterConfig AddGlobalReplaceNames(IEnumerable<KeyValuePair<string, string>> pairs);
        IObjectConverterConfig AddIgnoreProperties(IEnumerable<string> names);
        IObjectConverterConfig AddSpanConverters(IEnumerable<KeyValuePair<string, SpanConverter>> converterPairs);
    }

    internal sealed class ObjectConverterConfiguration : IObjectConverterConfig
    {
        readonly Dictionary<string, Type> _convertProps;
        //readonly Dictionary<string, string> _globalReplaceNames;
        JsonNameHolder _globalReplaceNames;
        readonly HashSet<string> _ignoreProps;
        readonly Dictionary<string, SpanConverter> _spanConverters;

        internal IReadOnlyDictionary<string, Type> ConvertProperties => _convertProps;
        internal JsonNameHolder GlobalReplaceNames => _globalReplaceNames;
        internal IReadOnlySet<string> IgnoreProperties => _ignoreProps;
        internal IMetadataResolver Resolver { get; }
        internal IReadOnlyDictionary<string, SpanConverter> SpanConverters => _spanConverters;

        internal ObjectConverterConfiguration(IMetadataResolver resolver)
        {
            this.Resolver = resolver;
            _convertProps = new(StringComparer.InvariantCultureIgnoreCase);
            _globalReplaceNames = default;
            _ignoreProps = new(_convertProps.Comparer);
            _spanConverters = new(_convertProps.Comparer);
        }

        public IObjectConverterConfig AddConvertProperties(IEnumerable<KeyValuePair<string, Type>> pairs)
        {
            ArgumentNullException.ThrowIfNull(pairs);
            foreach (var pair in pairs)
            {
                _convertProps.Add(pair.Key, pair.Value);
            }

            return this;
        }
        public IObjectConverterConfig AddGlobalReplaceNames(IEnumerable<KeyValuePair<string, string>> pairs)
        {
            ArgumentNullException.ThrowIfNull(pairs);
            _globalReplaceNames = JsonNameHolder.FromDeserializationNamePairs(pairs);
            return this;
        }
        public IObjectConverterConfig AddIgnoreProperties(IEnumerable<string> names)
        {
            ArgumentNullException.ThrowIfNull(names);
            _ignoreProps.UnionWith(names);
            return this;
        }
        public IObjectConverterConfig AddSpanConverters(IEnumerable<KeyValuePair<string, SpanConverter>> converterPairs)
        {
            ArgumentNullException.ThrowIfNull(converterPairs);
            foreach (var kvp in converterPairs)
            {
                _spanConverters.Add(kvp.Key, kvp.Value);
            }

            return this;
        }
    }
}
