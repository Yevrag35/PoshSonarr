using System.Collections;

namespace MG.Sonarr.Next.Collections
{
    internal readonly struct EmptyNameDictionary<T> : IEquatable<EmptyNameDictionary<T>>, IReadOnlyDictionary<string, T>, IReadOnlySet<T>
    {
        T IReadOnlyDictionary<string, T>.this[string key] => throw new KeyNotFoundException("This is a read-only, empty collection.");

        public int Count => 0;
        public IEnumerable<string> Keys => Enumerable.Empty<string>();
        public IEnumerable<T> Values => Enumerable.Empty<T>();

        public bool Contains(T key)
        {
            return false;
        }
        public bool ContainsKey(string key)
        {
            return false;
        }
        public bool Equals(EmptyNameDictionary<T> other)
        {
            return true;
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is EmptyNameDictionary<T> empty && this.Equals(empty);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            return Enumerable.Empty<KeyValuePair<string, T>>().GetEnumerator();
        }
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out T value)
        {
            value = default;
            return false;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return false;
        }
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return false;
        }
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return true;
        }
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return true;
        }
        public bool Overlaps(IEnumerable<T> other)
        {
            return false;
        }
        public bool SetEquals(IEnumerable<T> other)
        {
            return other is not null && !other.Any();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return Enumerable.Empty<T>().GetEnumerator();
        }

        internal static EmptyNameDictionary<T> Default => default;
    }
}
