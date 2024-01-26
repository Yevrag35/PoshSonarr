using System.Collections;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Extensions
{
    public static class HashtableExtensions
    {
        public static KeyValueEnumerator<TKey, TValue> EnumerateAsPairs<TKey, TValue>(this IDictionary table)
        {
            ArgumentNullException.ThrowIfNull(table);
            return new KeyValueEnumerator<TKey, TValue>(table);
        }

        public ref struct KeyValueEnumerator<TKey, TValue>
        {
            IDictionaryEnumerator? _enumerator;

            public KeyValuePair<TKey, TValue> Current { get; private set; }

            public KeyValueEnumerator(IDictionary dictionary)
            {
                _enumerator = dictionary.GetEnumerator();
                this.Current = default;
            }

            public readonly KeyValueEnumerator<TKey, TValue> GetEnumerator() => this;

            public bool MoveNext()
            {
                if (_enumerator is null || !_enumerator.MoveNext())
                {
                    return false;
                }
                
                if (_enumerator.Key is not TKey tKey)
                {
                    throw new InvalidKeyTypeException(typeof(TKey), _enumerator.Key);
                }

                if (_enumerator.Value is not TValue tValue)
                {
                    throw new InvalidValueTypeException(typeof(TValue), _enumerator.Value);
                }
                
                this.Current = new(tKey, tValue);
                return true;
            }

            public void Dispose()
            {
                _enumerator = null!;
                this = default;
            }

            public readonly void Reset()
            {
                _enumerator?.Reset();
            }
        }
    }
}

