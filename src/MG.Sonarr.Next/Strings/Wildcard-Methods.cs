using System.Collections;

namespace MG.Sonarr.Next.Strings;

public readonly partial struct Wildcard
{
	/// <summary>
	/// Creates a new read-only span over the <see cref="Wildcard"/> object.
	/// </summary>
	/// <returns>The read-only span representation of the <see cref="Wildcard"/>.</returns>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<char> AsSpan()
	{
		return _pattern.AsSpan();
	}
	/// <summary>
	/// Creates a new read-only span over a portion of the <see cref="Wildcard"/> object from a specified
	/// position to the end of the string.
	/// </summary>
	/// <param name="start">The zero-based index at which to begin this slice.</param>
	/// <returns>The read-only span representation of the <see cref="Wildcard"/>.</returns>
	/// <inheritdoc cref="MemoryExtensions.AsSpan(string?, int)" path="/exception[1]"/>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<char> AsSpan(int start)
	{
		return _pattern.AsSpan(start);
	}
	/// <summary>
	/// Creates a new read-only span over a portion of the <see cref="Wildcard"/> object from a specified
	/// position for a specified number of characters.
	/// </summary>
	/// <param name="start">The zero-based index at which to begin this slice.</param>
	/// <param name="length">The desired length for the slice.</param>
	/// <returns>The read-only span representation of the <see cref="Wildcard"/>.</returns>
	/// <inheritdoc cref="MemoryExtensions.AsSpan(string?, int, int)" path="/exception"/>
	[DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<char> AsSpan(int start, int length)
	{
		return _pattern.AsSpan(start, length);
	}

	/// <summary>
	/// Returns an enumerator that iterates through this <see cref="Wildcard"/> instance's <see cref="char"/> elements.
	/// </summary>
	/// <returns>
	/// An enumerator that can be used to iterate through the <see cref="Wildcard"/> instance's <see cref="char"/> elements.
	/// </returns>
	[DebuggerStepThrough]
	public Enumerator GetEnumerator()
	{
		return new(this);
	}
	/// <inheritdoc/>
	[DebuggerStepThrough]
	IEnumerator<char> IEnumerable<char>.GetEnumerator()
	{
		return _pattern is not null
			? _pattern.GetEnumerator()
			: string.Empty.GetEnumerator();
	}
	/// <inheritdoc/>
	[DebuggerStepThrough]
	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<char>)this).GetEnumerator();
	}

	#region MATCHING

	/// <summary>
	/// Determines if the input <see cref="string"/> object matches the current 
	/// <see cref="Wildcard"/> object based on traditional wildcard pattern rules.
	/// </summary>
	/// <remarks>
	///     Traditional pattern matching includes:
	///     <para>
	///         <code>* - 0 or more of any character.<br/>
	///     ? - exactly 1 of any character.</code>
	///     </para>
	/// </remarks>
	/// <param name="input">The string to pattern match.</param>
	/// <returns>
	///     <see langword="true"/> if <paramref name="input"/> matches the <see cref="Wildcard"/> pattern;
	///     otherwise, <see langword="false"/>.
	/// </returns>
	[DebuggerStepThrough]
	public bool IsMatch(string? input)
	{
		return this.IsMatch(input.AsSpan(), DEFAULT_COMPARISON);
	}
	/// <summary>
	/// Determines if the specified <see cref="char"/> span matches the current 
	/// <see cref="Wildcard"/> object based on traditional wildcard pattern rules.
	/// </summary>
	/// <remarks>
	///     Traditional pattern matching includes:
	///     <para>
	///         <code>* - 0 or more of any character.<br/>
	///     ? - exactly 1 of any character.</code>
	///     </para>
	/// </remarks>
	/// <param name="input">The string to pattern match.</param>
	/// <returns>
	///     <see langword="true"/> if <paramref name="input"/> matches the <see cref="Wildcard"/> pattern;
	///     otherwise, <see langword="false"/>.
	/// </returns>
	public readonly bool IsMatch(ReadOnlySpan<char> input)
	{
		return this.IsMatch(input, DEFAULT_COMPARISON);
	}
	/// <summary>
	/// Determines if the specified <see cref="char"/> span matches the current 
	/// <see cref="Wildcard"/> object based on traditional wildcard pattern rules while using the specified
	/// string comparison rules
	/// </summary>
	/// <remarks>
	///     Traditional pattern matching includes:
	///     <para>
	///         <code>* - 0 or more of any character.<br/>
	///     ? - exactly 1 of any character.</code>
	///     </para>
	/// </remarks>
	/// <param name="input">The string to pattern match.</param>
	/// <param name="comparisonType">The comparison rules to use when comparing the pattern against the input.</param>
	/// <returns>
	///     <see langword="true"/> if <paramref name="input"/> matches the <see cref="Wildcard"/> pattern;
	///     otherwise, <see langword="false"/>.
	/// </returns>
	public readonly bool IsMatch(ReadOnlySpan<char> input, StringComparison comparisonType)
	{
		WildcardMatchType matchType = _state.Type;
		ReadOnlySpan<char> pattern = _pattern;

		return matchType switch
		{
			WildcardMatchType.All => true,
			WildcardMatchType.Exact => pattern.Equals(input, comparisonType),
			WildcardMatchType.Like => IsMatch(pattern, input, comparisonType),
			WildcardMatchType.StartsWith => input.StartsWith(pattern.TrimEnd('*'), comparisonType),
			WildcardMatchType.EndsWith => input.EndsWith(pattern.TrimStart('*'), comparisonType),
			WildcardMatchType.None or _ => false,
		};
	}

	#endregion

	#region PRIVATE METHODS

	//[DebuggerStepThrough]
	private static bool AreCharactersEqual(char x, char y, StringComparison comparisonType)
	{
		switch (comparisonType)
		{
			case StringComparison.CurrentCulture:
				goto case StringComparison.Ordinal;

			case StringComparison.CurrentCultureIgnoreCase:
				return char.ToUpper(x) == char.ToUpper(y);

			case StringComparison.InvariantCulture:
				goto case StringComparison.Ordinal;

			case StringComparison.InvariantCultureIgnoreCase:
				return char.ToUpperInvariant(x) == char.ToUpperInvariant(y);

			case StringComparison.Ordinal:
				return x == y;

			case StringComparison.OrdinalIgnoreCase:
			default:
				return x == y || AreCharactersEqualIgnoreCase(x, y);
		}
	}
	private static bool AreCharactersEqualIgnoreCase(char x, char y)
	{
		if (CharCollections.AlphaLowercase.Contains(x) && CharCollections.AlphaUppercase.Contains(y))
		{
			return x - 32 == y;
		}
		else if (CharCollections.AlphaUppercase.Contains(x) && CharCollections.AlphaLowercase.Contains(y))
		{
			return x == y - 32;
		}

		return x == y;
	}
	private static string ConstructPattern(string? patternString, WildcardMatchType matchType, ref int length)
	{
		if (patternString is null)
		{
			return string.Empty;
		}

		string resulting_pattern;

		switch (matchType)
		{
			case WildcardMatchType.None:
			default:
				length = 0;
				resulting_pattern = string.Empty;
				break;

			case WildcardMatchType.Like:
			case WildcardMatchType.StartsWith:
			case WildcardMatchType.EndsWith:
			case WildcardMatchType.Exact:
				length = patternString.Length;
				resulting_pattern = patternString;
				break;

			case WildcardMatchType.All:
				length = 1;
				resulting_pattern = ALL_STRING;
				break;
		}

		return resulting_pattern;
	}
	private static string ConstructPattern(ReadOnlySpan<char> pattern, WildcardMatchType matchType, ref int length)
	{
		string resulting_pattern;

		switch (matchType)
		{
			case WildcardMatchType.None:
			default:
				length = 0;
				resulting_pattern = string.Empty;
				break;

			case WildcardMatchType.Like:
			case WildcardMatchType.StartsWith:
			case WildcardMatchType.EndsWith:
			case WildcardMatchType.Exact:
				length = pattern.Length;
				resulting_pattern = new(pattern);
				break;

			case WildcardMatchType.All:
				length = 1;
				resulting_pattern = ALL_STRING;
				break;
		}

		return resulting_pattern;
	}
	private static WildcardMatchType DeterminePattern(ReadOnlySpan<char> pattern)
	{
		if (pattern.IsEmpty)
		{
			return WildcardMatchType.None;
		}

		if (TryAsSingleCharacterPattern(pattern, out WildcardMatchType matchType))
		{
			return matchType;
		}

		if (!pattern.ContainsAny(_wildcardChars))
		{
			return WildcardMatchType.Exact;
		}

		matchType = WildcardMatchType.Like;
		if ('*' == pattern[^1] && !pattern.TrimEnd('*').ContainsAny(_wildcardChars))
		{
			matchType = WildcardMatchType.StartsWith;
		}
		else if ('*' == pattern[0] && !pattern.TrimStart('*').ContainsAny(_wildcardChars))
		{
			matchType = WildcardMatchType.EndsWith;
		}

		return matchType;
	}
	private static bool IsMatch(ReadOnlySpan<char> pattern, ReadOnlySpan<char> input, StringComparison comparisonType)
	{
		int starIndex = -1;
		int iIndex = -1;

		int i = 0;
		int j = 0;

		while ((uint)i < (uint)input.Length)
		{
			if ((uint)j < (uint)pattern.Length && (pattern[j] == '?' || AreCharactersEqual(pattern[j], input[i], comparisonType)))
			{
				i++;
				j++;
			}
			else if ((uint)j < (uint)pattern.Length && pattern[j] == '*')
			{
				starIndex = j;
				iIndex = i;
				j++;
			}
			else if (starIndex == -1)
			{
				return false;
			}
			else
			{
				j = starIndex + 1;
				i = iIndex + 1;
				iIndex++;
			}
		}

		while ((uint)j < (uint)pattern.Length && pattern[j] == '*')
		{
			j++;
		}

		return j == pattern.Length;
	}
	private static bool TryAsSingleCharacterPattern(ReadOnlySpan<char> pattern, out WildcardMatchType matchType)
	{
		if (pattern.Length == 1)
		{
			matchType = pattern[0] switch
			{
				'*' => WildcardMatchType.All,
				'?' => WildcardMatchType.Like,
				_ => WildcardMatchType.Exact,
			};

			return true;
		}

		matchType = default;
		return false;
	}

	#endregion

	[StructLayout(LayoutKind.Auto)]
	public ref struct Enumerator
	{
		private int _index;
		private readonly int _length;
		private char _current;
		private readonly ref char _start;

		public readonly char Current => _current;

		internal Enumerator(Wildcard wildcard)
		{
			_index = -1;
			_length = wildcard._state.Length;
			_start = ref MemoryMarshal.GetReference<char>(wildcard);
			_current = default;
		}

		public bool MoveNext()
		{
			int newIndex = _index + 1;
			if ((uint)newIndex < (uint)_length)
			{
				_index = newIndex;
				_current = Unsafe.Add(ref _start, _index);
				return true;
			}

			_index = _length;
			return false;
		}
	}
}