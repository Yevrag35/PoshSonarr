using MG.Sonarr.Next.Extensions;
using System.Collections.ObjectModel;

namespace MG.Sonarr.Next.Collections
{
    internal readonly struct JsonNameHolder
    {
        readonly IReadOnlyDictionary<string, string> _deserialization;
        readonly IReadOnlyDictionary<string, string> _serialization;

        public IReadOnlyDictionary<string, string> DeserializationNames => _deserialization ?? EmptyNameDictionary<string>.Default;
        public IReadOnlyDictionary<string, string> SerializationNames => _serialization ?? EmptyNameDictionary<string>.Default;

        internal JsonNameHolder(IReadOnlyDictionary<string, string>? serialization, IReadOnlyDictionary<string, string>? deserialization)
        {
            _deserialization = deserialization ?? EmptyNameDictionary<string>.Default;
            _serialization = serialization ?? EmptyNameDictionary<string>.Default;
        }

        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        internal static JsonNameHolder FromSingleNamePair(string key, string value)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            ArgumentException.ThrowIfNullOrEmpty(value);

            OneStringDictionary des = new(key, value);
            OneStringDictionary ser = new(value, key);

            return new(ser, des);
        }
        internal static JsonNameHolder FromDeserializationNamePairs(IEnumerable<KeyValuePair<string, string>> values)
        {
            if (values is IReadOnlyDictionary<string, string> dict)
            {
                return FromSingleDictionary(dict);
            }
            else if (values.TryGetNonEnumeratedCount(out int count) && count == 1)
            {
                var deserOne = OneStringDictionary.FromEnumerable(values);
                return new(new OneStringDictionary(deserOne.Value, deserOne.Key), deserOne);
            }
            else
            {
                Dictionary<string, string> des = new(count, StringComparer.InvariantCultureIgnoreCase);
                Dictionary<string, string> ser = new(count, StringComparer.InvariantCultureIgnoreCase);
                foreach (KeyValuePair<string, string> kvp in values)
                {
                    des.Add(kvp.Key, kvp.Value);
                    ser.Add(kvp.Value, kvp.Key);
                }

                return new(ser.ToReadOnly(), des.ToReadOnly());
            }
        }

        private static JsonNameHolder FromSingleDictionary(IReadOnlyDictionary<string, string> dict)
        {
            return dict.Count switch
            {
                0 => new(EmptyNameDictionary<string>.Default, EmptyNameDictionary<string>.Default),
                1 => new(OneStringDictionary.FromEnumerable(dict), dict),
                >= 2 => new(ReverseDictionary(dict), dict),
                _ => throw new UnreachableException("How could you have a negative count???"),
            };
        }

        private static IReadOnlyDictionary<string, string> ReverseDictionary(IReadOnlyDictionary<string, string> forwardDict)
        {
            IEqualityComparer<string> comparer = forwardDict is Dictionary<string, string> realDict
                ? realDict.Comparer
                : StringComparer.InvariantCultureIgnoreCase;

            Dictionary<string, string> reversed = new(forwardDict.Count, comparer);
            foreach (var kvp in forwardDict)
            {
                reversed.Add(kvp.Value, kvp.Key);
            }

            return reversed.ToReadOnly();
        }
    }
}
