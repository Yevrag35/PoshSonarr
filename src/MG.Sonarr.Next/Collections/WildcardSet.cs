using MG.Sonarr.Next.Strings;
using Microsoft.Extensions.ObjectPool;
using System.Collections;
using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Collections;

[DebuggerDisplay("Count = {Count}")]
[CollectionBuilder(typeof(WildcardSet), nameof(Create))]
public sealed class WildcardSet : IReadOnlyCollection<Wildcard>, IResettable
{
    private const int DEFAULT_CAPACITY = 5;
    private static readonly WildcardEqualityComparer _comparer = new();

    private readonly HashSet<Wildcard> _set;

    /// <summary>
    /// Gets the number of wildcard patterns are contained in the <see cref="WildcardSet"/>.
    /// </summary>
    public int Count
    {
        [DebuggerStepThrough]
        get => _set.Count;
    }

    [DebuggerStepThrough]
    public WildcardSet()
        : this(DEFAULT_CAPACITY)
    {
    }
    private WildcardSet(scoped ReadOnlySpan<Wildcard> values)
        : this(values.Length)
    {
        foreach (Wildcard ws in values)
        {
            _ = _set.Add(ws);
        }
    }
    [DebuggerStepThrough]
    private WildcardSet(int capacity)
    {
        _set = new(capacity, _comparer);
#if NET9_0_OR_GREATER
        _alternate = _set.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    private readonly HashSet<Wildcard>.AlternateLookup<ReadOnlySpan<char>> _alternate;
#else
    }
#endif

    public bool Add(Wildcard value)
    {
        return _set.Add(value);
    }
    public bool Add([NotNullWhen(true)] string? value)
    {
        Wildcard wildcard = Wildcard.Parse(value);
        return !wildcard.IsEmpty && _set.Add(wildcard);
    }
    public bool Add(int value)
    {
        Span<char> chars = stackalloc char[LengthConstants.INT_MAX];
        _ = value.TryFormat(chars, out int written);
        chars = chars.Slice(0, written);
#if NET9_0_OR_GREATER

        return this.Add(chars);
    }
    public bool Add(ReadOnlySpan<char> value)
    {
        if (value.IsWhiteSpace())
        {
            return false;
        }

        return _alternate.Add(value);
    }
    public bool Contains(ReadOnlySpan<char> value)
    {
        return _alternate.Contains(value);
    }

#else
        return this.Add(Wildcard.Parse(chars));
    }
#endif
    public bool Contains(Wildcard value)
    {
        return _set.Contains(value);
    }

    /// <summary>
    /// Removes all elements from the <see cref="WildcardSet"/>.
    /// </summary>
    public void Clear()
    {
        _set.Clear();
    }

    /// <summary>
    /// Determines if any of the <see cref="Wildcard"/> objects in the current set match the provided value.
    /// </summary>
    /// <param name="value">The read-only span of characters to match against.</param>
    /// <returns>
    /// <see langword="true"/> if any of the <see cref="Wildcard"/> objects in the current set match the provided value;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsAnyMatch(ReadOnlySpan<char> value)
    {
        foreach (Wildcard wc in _set)
        {
            if (wc.IsMatch(value))
                return true;
        }

        return false;
    }
    /// <summary>
    /// Determines if any of the <see cref="Wildcard"/> objects in the current set match the provided value.
    /// </summary>
    /// <param name="value">The string to match against.</param>
    /// <returns>
    /// <see langword="true"/> if any of the <see cref="Wildcard"/> objects in the current set match the provided value;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsAnyMatch([DisallowNull] string value)
    {
        return _set.Any(ws => ws.IsMatch(value));
    }
    /// <summary>
    /// Modifies the current <see cref="WildcardSet"/> object to contain only the elements that are present in itself,
    /// the specified collection, or both.
    /// </summary>
    /// <param name="other">The collection to compare to the current <see cref="WildcardSet"/> object.</param>
    public void UnionWith(IEnumerable<Wildcard> other)
    {
        _set.UnionWith(other);
    }
    /// <summary>
    /// Modifies the current <see cref="WildcardSet"/> object to contain only the elements that are present in itself,
    /// the specified collection, or both.
    /// </summary>
    /// <param name="other">The collection of string patterns to compare to the current <see cref="WildcardSet"/> object.</param>
    public void UnionWith(IEnumerable<string> other)
    {
        this.UnionWith(other.Select(Wildcard.Parse));
    }

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="WildcardSet"/>.
    /// </summary>
    [DebuggerStepThrough]
    public IEnumerator<Wildcard> GetEnumerator()
    {
        return _set.GetEnumerator();
    }
    /// <inheritdoc/>
    [DebuggerStepThrough]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    /// <inheritdoc/>
    bool IResettable.TryReset()
    {
        this.Clear();
        return true;
    }

    public static WildcardSet Create(params ReadOnlySpan<Wildcard> values)
    {
        return !values.IsEmpty
            ? new(values)
            : new();
    }

    private sealed class WildcardEqualityComparer : IEqualityComparer<Wildcard>
#if NET9_0_OR_GREATER
        , IAlternateEqualityComparer<ReadOnlySpan<char>, Wildcard>
#endif
    {
        public bool Equals(Wildcard x, Wildcard y)
        {
            return x.Equals(y);
        }
        public int GetHashCode([DisallowNull] Wildcard obj)
        {
            return obj.GetHashCode();
        }

#if NET9_0_OR_GREATER
        public Wildcard Create(ReadOnlySpan<char> alternate)
        {
            return Wildcard.Parse(alternate);
        }

        public bool Equals(ReadOnlySpan<char> alternate, Wildcard other)
        {
            return other.Equals(alternate);
        }

        public int GetHashCode(ReadOnlySpan<char> alternate)
        {
            return ((IAlternateEqualityComparer<ReadOnlySpan<char>, string>)StringComparer.OrdinalIgnoreCase)
                .GetHashCode(alternate);
        }
#endif
    }
}