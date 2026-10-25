using MG.Sonarr.Next.Collections.Pools;
using MG.Sonarr.Next.Strings;
using System.Buffers;
using System.Collections;

namespace MG.Sonarr.Next.Collections;

/// <summary>
/// Represents a set of wildcard patterns.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
[CollectionBuilder(typeof(WildcardSet), nameof(Create))]
public sealed class WildcardSet : IReadOnlyCollection<Wildcard>, IResettable
{
	private const int DEFAULT_CAPACITY = 5;
	private static readonly WildcardEqualityComparer s_comparer = new();

	private readonly int _capacity;
	private readonly HashSet<Wildcard> _set;

	/// <summary>
	/// Gets the number of wildcard patterns are contained in the <see cref="WildcardSet"/>.
	/// </summary>
	public int Count
	{
		[DebuggerStepThrough]
		get => _set.Count;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="WildcardSet"/> class with the default capacity.
	/// </summary>
	[DebuggerStepThrough]
	public WildcardSet()
		: this(DEFAULT_CAPACITY)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="WildcardSet"/> class with the specified values.
	/// </summary>
	/// <param name="values">The wildcard values to initialize the set with.</param>
	private WildcardSet(scoped ReadOnlySpan<Wildcard> values)
		: this(values.Length)
	{
		foreach (Wildcard wc in values)
		{
			_ = _set.Add(wc);
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="WildcardSet"/> class with the specified capacity.
	/// </summary>
	/// <param name="capacity">The initial capacity of the set.</param>
	[DebuggerStepThrough]
	private WildcardSet(int capacity)
	{
		_set = new(capacity, s_comparer);
#if NET9_0_OR_GREATER
		_alternate = _set.GetAlternateLookup<ReadOnlySpan<char>>();
	}

	private readonly HashSet<Wildcard>.AlternateLookup<ReadOnlySpan<char>> _alternate;
#else
    }
#endif

	/// <summary>
	/// Adds a wildcard to the set.
	/// </summary>
	/// <param name="value">The wildcard to add.</param>
	/// <returns><see langword="true"/> if the wildcard was added; otherwise, <see langword="false"/>.</returns>
	public bool Add(Wildcard value)
	{
		return _set.Add(value);
	}

	/// <summary>
	/// Adds a wildcard pattern to the set.
	/// </summary>
	/// <param name="value">The wildcard pattern to add.</param>
	/// <returns><see langword="true"/> if the wildcard pattern was added; otherwise, <see langword="false"/>.</returns>
	public bool Add([NotNullWhen(true)] string? value)
	{
		Wildcard wildcard = Wildcard.Parse(value);
		return !wildcard.IsEmpty && _set.Add(wildcard);
	}

	/// <summary>
	/// Adds an integer value as a wildcard pattern to the set.
	/// </summary>
	/// <param name="value">The integer value to add.</param>
	/// <returns><see langword="true"/> if the integer value was added; otherwise, <see langword="false"/>.</returns>
	public bool Add(int value)
	{
		Span<char> chars = stackalloc char[LengthConstants.INT_MAX];
		_ = value.TryFormat(chars, out int written);
		chars = chars.Slice(0, written);
#if NET9_0_OR_GREATER

		return this.Add(chars);
	}

	/// <summary>
	/// Adds a read-only span of characters as a wildcard pattern to the set.
	/// </summary>
	/// <param name="value">The read-only span of characters to add.</param>
	/// <returns><see langword="true"/> if the span was added; otherwise, <see langword="false"/>.</returns>
	public bool Add(ReadOnlySpan<char> value)
	{
		if (value.IsWhiteSpace())
		{
			return false;
		}

		return _alternate.Add(value);
	}

	/// <summary>
	/// Determines whether the set contains a specific read-only span of characters.
	/// </summary>
	/// <param name="value">The read-only span of characters to locate in the set.</param>
	/// <returns><see langword="true"/> if the span is found; otherwise, <see langword="false"/>.</returns>
	public bool Contains(ReadOnlySpan<char> value)
	{
		return _alternate.Contains(value);
	}

#else
        return this.Add(Wildcard.Parse(chars));
    }
#endif

	/// <summary>
	/// Determines whether the set contains a specific wildcard.
	/// </summary>
	/// <param name="value">The wildcard to locate in the set.</param>
	/// <returns><see langword="true"/> if the wildcard is found; otherwise, <see langword="false"/>.</returns>
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
		int count = _set.Count;
		Wildcard[] array = ArrayPool<Wildcard>.Shared.Rent(count);
		try
		{
			_set.CopyTo(array);
			ref Wildcard first = ref MemoryMarshal.GetArrayDataReference(array);

			for (int i = 0; i < count; i++)
			{
				if (Unsafe.Add(ref first, i).IsMatch(value))
					return true;
			}

			return false;
		}
		finally
		{
			ArrayPool<Wildcard>.Shared.Return(array);
		}
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
		ReadOnlySpan<char> chars = value;
		foreach (Wildcard str in _set)
		{
			if (str.IsMatch(chars, StringComparison.OrdinalIgnoreCase))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Removes the specified wildcard from the set if it exists.
	/// </summary>
	/// <param name="value">The wildcard to remove from the set. Cannot be null.</param>
	/// <returns><see langword="true"/> if the wildcard was successfully removed; otherwise, <see langword="false"/>.</returns>
	public bool Remove(Wildcard value)
	{
		return _set.Remove(value);
	}

	/// <summary>
	/// Modifies the current <see cref="WildcardSet"/> object to contain only the elements that are present in itself,
	/// the specified collection, or both.
	/// </summary>
	/// <param name="other">The collection to compare to the current <see cref="WildcardSet"/> object.</param>
	public void UnionWith(IEnumerable<Wildcard> other)
	{
		if (other is WildcardSet wcSet)
		{
			_set.UnionWith(wcSet._set);
		}
		else
		{
			_set.UnionWith(other);
		}
	}

	/// <summary>
	/// Modifies the current <see cref="WildcardSet"/> object to contain only the elements that are present in itself,
	/// the specified collection, or both.
	/// </summary>
	/// <param name="other">The collection of string patterns to compare to the current <see cref="WildcardSet"/> object.</param>
	public void UnionWith(IEnumerable<string> other)
	{
		foreach (string s in other)
		{
			if (s is null)
				continue;

			_ = _set.Add(s);
		}
	}

	/// <summary>
	/// Returns an enumerator that iterates through the <see cref="WildcardSet"/>.
	/// </summary>
	[DebuggerStepThrough]
	public Enumerator GetEnumerator()
	{
		return new(_set);
	}

	/// <inheritdoc/>
	[DebuggerStepThrough]
	IEnumerator<Wildcard> IEnumerable<Wildcard>.GetEnumerator()
	{
		return this.GetEnumerator();
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

	/// <summary>
	/// Creates a new <see cref="WildcardSet"/> with the specified values.
	/// </summary>
	/// <param name="values">The wildcard values to initialize the set with.</param>
	/// <returns>A new <see cref="WildcardSet"/> containing the specified values.</returns>
	public static WildcardSet Create(params ReadOnlySpan<Wildcard> values)
	{
		return !values.IsEmpty
			? new(values)
			: new();
	}

	[StructLayout(LayoutKind.Auto)]
	public struct Enumerator : IEnumerator<Wildcard>
	{
		private HashSet<Wildcard> _set;
		private HashSet<Wildcard>.Enumerator _enumerator;
		private Wildcard _current;

		internal Enumerator(HashSet<Wildcard> set)
		{
			_set = set;
			_enumerator = set.GetEnumerator();
			_current = default;
		}

		public readonly Wildcard Current => _current;
		readonly object? IEnumerator.Current => this.Current;

		public void Dispose()
		{
			this = default;
		}

		public bool MoveNext()
		{
			if (_enumerator.MoveNext())
			{
				_current = _enumerator.Current;
				return true;
			}

			return false;
		}

		void IEnumerator.Reset()
		{
			var enumerator = _enumerator;
			((IEnumerator)enumerator).Reset();
			_enumerator = enumerator;
		}
	}

	private sealed class WildcardEqualityComparer : IEqualityComparer<Wildcard>, IAlternateEqualityComparer<ReadOnlySpan<char>, Wildcard>,
		IAlternateEqualityComparer<string, Wildcard>
	{
		/// <summary>
		/// Creates a wildcard from the specified read-only span of characters.
		/// </summary>
		/// <param name="alternate">The read-only span of characters to create the wildcard from.</param>
		/// <returns>A new wildcard created from the specified span.</returns>
		public Wildcard Create(ReadOnlySpan<char> alternate)
		{
			return Wildcard.Parse(alternate);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="alternate"></param>
		/// <returns></returns>
		public Wildcard Create(string alternate)
		{
			return Wildcard.Parse(alternate);
		}

		/// <summary>
		/// Determines whether the specified read-only span of characters and wildcard are equal.
		/// </summary>
		/// <param name="alternate">The read-only span of characters to compare.</param>
		/// <param name="other">The wildcard to compare.</param>
		/// <returns><see langword="true"/> if the specified span and wildcard are equal; otherwise, <see langword="false"/>.</returns>
		public bool Equals(ReadOnlySpan<char> alternate, Wildcard other)
		{
			return other.Equals(alternate, StringComparison.OrdinalIgnoreCase);
		}
		///
		public bool Equals(string alternate, Wildcard other)
		{
			return other.Equals(alternate, StringComparison.OrdinalIgnoreCase);
		}
		/// <summary>
		/// Determines whether the specified wildcards are equal.
		/// </summary>
		/// <param name="x">The first wildcard to compare.</param>
		/// <param name="y">The second wildcard to compare.</param>
		/// <returns><see langword="true"/> if the specified wildcards are equal; otherwise, <see langword="false"/>.</returns>
		public bool Equals(Wildcard x, Wildcard y)
		{
			return x.Equals(y);
		}

		/// <summary>
		/// Returns a hash code for the specified read-only span of characters.
		/// </summary>
		/// <param name="alternate">The read-only span of characters for which a hash code is to be returned.</param>
		/// <returns>A hash code for the specified span.</returns>
		public int GetHashCode(ReadOnlySpan<char> alternate)
		{
			return string.GetHashCode(alternate, StringComparison.OrdinalIgnoreCase);
		}

		public int GetHashCode([DisallowNull] string alternate)
		{
			return alternate.GetHashCode(StringComparison.OrdinalIgnoreCase);
		}
		/// <summary>
		/// Returns a hash code for the specified wildcard.
		/// </summary>
		/// <param name="obj">The wildcard for which a hash code is to be returned.</param>
		/// <returns>A hash code for the specified wildcard.</returns>
		public int GetHashCode(Wildcard obj)
		{
			return obj.GetHashCode();
		}
	}
}