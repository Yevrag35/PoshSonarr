namespace MG.Sonarr.Next.Collections
{
    internal sealed class NameLookup<T> where T : notnull, IComparable<T>
    {
        readonly IComparer<T>? _setComparer;
        readonly Dictionary<T, SortedSet<T>> _dict;

        internal int Count => _dict.Count;

        internal IReadOnlySet<T> this[T key]
        {
            get => _dict.TryGetValue(key, out SortedSet<T>? result)
                ? result
                : EmptyNameDictionary<T>.Default;
        }

        internal NameLookup(int capacity, IEqualityComparer<T>? equalityComparer, IComparer<T>? comparer)
        {
            _dict = new(capacity, equalityComparer);
            _setComparer = comparer;
        }

        internal bool Add(T key, T value)
        {
            if (!_dict.TryGetValue(key, out SortedSet<T>? set))
            {
                set = new(_setComparer);
                _dict.Add(key, set);
            }

            return set.Add(value);
        }
    }
}
