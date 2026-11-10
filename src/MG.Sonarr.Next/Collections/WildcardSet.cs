using MG.Sonarr.Next.Strings;

namespace MG.Sonarr.Next.Collections;

/// <summary>
/// Represents a set of wildcard patterns.
/// </summary>
/// <remarks>
///     <para>
///         Two patterns are the same item when their text is equal under
///         <see cref="StringComparison.OrdinalIgnoreCase"/>, whichever overload adds or looks them up, so
///         <see cref="Add(Wildcard)"/>, <see cref="Add(string)"/>, <see cref="Add(ReadOnlySpan{char})"/>, and
///         <see cref="Add(int)"/> keep one item for <c>abc</c>, <c>ABC</c>, or <c>5</c> and <c>"5"</c>.
///     </para>
///     <para>
///         Empty text is a name that matches nothing: every member that adds it adds <see cref="Wildcard.Empty"/>, which
///         <see cref="IsAnyMatch(ReadOnlySpan{char})"/> never matches, so a set of empty names isn't empty and matches no
///         input. Only a <see langword="null"/> string adds nothing. Text of white space alone is an exact pattern,
///         which matches only the same white space.
///     </para>
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
[CollectionBuilder(typeof(WildcardSet), nameof(Create))]
public sealed class WildcardSet : IReadOnlyCollection<Wildcard>
{
	private const int DEFAULT_CAPACITY = 5;
	private static readonly WildcardEqualityComparer s_comparer = new();

	//private readonly int _capacity;
	private readonly HashSet<Wildcard> _set;

	/// <summary>
	/// Gets the number of wildcard patterns in the <see cref="WildcardSet"/>.
	/// </summary>
	/// <value>The number of wildcard patterns in the set.</value>
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
	/// <remarks>
	/// Empty text adds <see cref="Wildcard.Empty"/>, which matches nothing.
	/// </remarks>
	/// <param name="value">The wildcard pattern to add.</param>
	/// <returns>
	/// <see langword="true"/> if the wildcard pattern was added; otherwise, <see langword="false"/>, when
	/// <paramref name="value"/> is <see langword="null"/> or the set already holds it.
	/// </returns>
	public bool Add([NotNullWhen(true)] string? value)
	{
		return value is not null && _set.Add(Wildcard.Parse(value));
	}

	/// <summary>
	/// Adds an integer value as a wildcard pattern to the set.
	/// </summary>
	/// <param name="value">The integer value to add.</param>
	/// <returns><see langword="true"/> if the integer value was added; otherwise, <see langword="false"/>.</returns>
	public bool Add(int value)
	{
		Span<char> chars = stackalloc char[LengthConstants.INT_MAX];
		bool result = false;
		if (value.TryFormat(chars, out int written, provider: CultureInfo.InvariantCulture))
		{
			result = this.Add(chars.UnsafeSlice(0, written));
		}

		return result;
	}

	/// <summary>
	/// Adds a read-only span of characters as a wildcard pattern to the set.
	/// </summary>
	/// <remarks>
	/// White space alone is an exact pattern, and empty text adds <see cref="Wildcard.Empty"/>, as
	/// <see cref="Add(string)"/> does.
	/// </remarks>
	/// <param name="value">The read-only span of characters to add.</param>
	/// <returns>
	/// <see langword="true"/> if the span was added; otherwise, <see langword="false"/>, when the set already holds its
	/// pattern.
	/// </returns>
	public bool Add(ReadOnlySpan<char> value)
	{
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
		foreach (Wildcard wildcard in _set)
		{
			if (wildcard.IsMatch(value))
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
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
	public void UnionWith(IEnumerable<Wildcard> other)
	{
		ArgumentNullException.ThrowIfNull(other);
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
	/// <remarks>
	/// Each string is added as <see cref="Add(string)"/> adds it, so a <see langword="null"/> one is skipped and an empty
	/// one adds <see cref="Wildcard.Empty"/>.
	/// </remarks>
	/// <param name="other">The collection of string patterns to compare to the current <see cref="WildcardSet"/> object.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
	public void UnionWith(IEnumerable<string> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		foreach (string s in other)
		{
			_ = this.Add(s);
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

	/// <summary>
	/// Enumerates elements of a <see cref="WildcardSet"/>.
	/// </summary>
	[StructLayout(LayoutKind.Auto)]
	public struct Enumerator : IEnumerator<Wildcard>
	{
		private HashSet<Wildcard>.Enumerator _enumerator;
		private Wildcard _current;

		/// <summary>
		/// Initializes a new <see cref="Enumerator"/> instance.
		/// </summary>
		/// <param name="set">The backing set to enumerate.</param>
		internal Enumerator(HashSet<Wildcard> set)
		{
			_enumerator = set.GetEnumerator();
			_current = default;
		}

		/// <summary>
		/// Gets the current wildcard.
		/// </summary>
		/// <value>The current wildcard element.</value>
		public readonly Wildcard Current => _current;
		readonly object? IEnumerator.Current => this.Current;

		/// <summary>
		/// Releases the enumerator resources.
		/// </summary>
		public void Dispose()
		{
			this = default;
		}

		/// <summary>
		/// Advances the enumerator to the next element.
		/// </summary>
		/// <returns><see langword="true"/> when the enumerator advanced to the next element; otherwise, <see langword="false"/>.</returns>
		public bool MoveNext()
		{
			if (_enumerator.MoveNext())
			{
				_current = _enumerator.Current;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Sets the enumerator to its initial position, before the first wildcard, and clears <see cref="Current"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the set changed after the enumerator was created, as <see cref="HashSet{T}"/>'s enumerator throws.
		/// </exception>
		void IEnumerator.Reset()
		{
			ResetInPlace(ref _enumerator);
			_current = default;
		}

		/// <summary>
		/// Resets the specified enumerator where it's stored, with a constrained call that doesn't box a copy.
		/// </summary>
		/// <typeparam name="TEnumerator">The enumerator's type.</typeparam>
		/// <param name="enumerator">The enumerator to reset.</param>
		private static void ResetInPlace<TEnumerator>(ref TEnumerator enumerator) where TEnumerator : struct, IEnumerator
		{
			enumerator.Reset();
		}
	}

	/// <summary>
	/// Provides wildcard equality and hash code semantics for set storage and alternate lookups.
	/// </summary>
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
		/// Creates a wildcard from the specified string.
		/// </summary>
		/// <param name="alternate">The string to create the wildcard from.</param>
		/// <returns>A new wildcard created from the specified string.</returns>
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
		/// <summary>
		/// Determines whether the specified string and wildcard are equal.
		/// </summary>
		/// <param name="alternate">The string to compare.</param>
		/// <param name="other">The wildcard to compare.</param>
		/// <returns><see langword="true"/> if the specified string and wildcard are equal; otherwise, <see langword="false"/>.</returns>
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

		/// <summary>
		/// Returns a hash code for the specified string.
		/// </summary>
		/// <param name="alternate">The string for which a hash code is to be returned.</param>
		/// <returns>A hash code for the specified string.</returns>
		public int GetHashCode([DisallowNull] string alternate)
		{
			return alternate.GetHashCode(StringComparison.OrdinalIgnoreCase);
		}
		/// <summary>
		/// Returns a hash code for the specified wildcard's pattern, ignoring case.
		/// </summary>
		/// <remarks>
		/// A pattern hashes as its text does in <see cref="GetHashCode(ReadOnlySpan{char})"/> and
		/// <see cref="GetHashCode(string)"/>, so text that equals a wildcard has the wildcard's hash code, as
		/// <see cref="IAlternateEqualityComparer{TAlternate, T}"/> requires. Two wildcards that
		/// <see cref="Equals(Wildcard, Wildcard)"/> finds equal have text that's equal ignoring case, so they hash alike too.
		/// </remarks>
		/// <param name="obj">The wildcard for which a hash code is to be returned.</param>
		/// <returns>A hash code for the specified wildcard.</returns>
		public int GetHashCode(Wildcard obj)
		{
			return string.GetHashCode(obj.AsSpan(), StringComparison.OrdinalIgnoreCase);
		}
	}
}