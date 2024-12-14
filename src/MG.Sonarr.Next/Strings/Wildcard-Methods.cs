using System.Collections;

namespace MG.Sonarr.Next.Strings;
public readonly partial struct Wildcard
{
	/// <summary>
	/// Creates a new read-only span over the <see cref="Wildcard"/> object.
	/// </summary>
	/// <returns>The read-only span representation of the <see cref="Wildcard"/>.</returns>
	[DebuggerStepThrough]
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
	[DebuggerStepThrough]
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
	[DebuggerStepThrough]
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
	public CharEnumerator GetEnumerator()
	{
		if (this.IsEmpty)
		{
			return string.Empty.GetEnumerator();
		}

		return _pattern.GetEnumerator();
	}
	/// <inheritdoc/>
	readonly IEnumerator<char> IEnumerable<char>.GetEnumerator()
	{
		return this.GetEnumerator();
	}
	/// <inheritdoc/>
	[DebuggerStepThrough]
	readonly IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
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
		WildcardMatchType matchType = _matchType;
		ReadOnlySpan<char> pattern = _pattern;

		return matchType switch
		{
			WildcardMatchType.All => true,
			WildcardMatchType.Exact => pattern.Equals(input, comparisonType),
			WildcardMatchType.Like => IsMatch(pattern, input, in comparisonType),
			WildcardMatchType.StartsWith => input.StartsWith(pattern.TrimEnd('*'), comparisonType),
			WildcardMatchType.EndsWith => input.EndsWith(pattern.TrimStart('*'), comparisonType),
			WildcardMatchType.None or _ => false,
		}; 
	}

	#endregion

	#region PRIVATE METHODS

	//[DebuggerStepThrough]
	private static bool AreCharactersEqual(in char x, in char y, in StringComparison comparisonType)
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
				return x == y || AreCharactersEqualIgnoreCase(in x, in y);
		}
	}
	private static bool AreCharactersEqualIgnoreCase(in char x, in char y)
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
	private static string ConstructPattern(ReadOnlySpan<char> pattern, in WildcardMatchType matchType, ref int length, ref bool isNotEmpty)
	{
		string resulting_pattern;

		switch (matchType)
		{
			case WildcardMatchType.None:
			default:
				length = 0;
				isNotEmpty = false;
				resulting_pattern = string.Empty;
				break;

			case WildcardMatchType.Like:
			case WildcardMatchType.StartsWith:
			case WildcardMatchType.EndsWith:
			case WildcardMatchType.Exact:
				length = pattern.Length;
				isNotEmpty = true;
				resulting_pattern = pattern.ToString();
				break;

			//case WildcardMatchType.StartsWith:
			//	pattern = pattern.TrimEnd('*');
			//	goto case WildcardMatchType.Exact;

			//case WildcardMatchType.EndsWith:
			//	pattern = pattern.TrimStart('*');
			//	goto case WildcardMatchType.Exact;

			case WildcardMatchType.All:
				length = 1;
				isNotEmpty = true;
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
	private static bool IsMatch(ReadOnlySpan<char> pattern, ReadOnlySpan<char> input, in StringComparison comparisonType)
	{
		int starIndex = -1;
		int iIndex = -1;

		int i = 0;
		int j = 0;

		while ((uint)i < (uint)input.Length)
		{
			if ((uint)j < (uint)pattern.Length && (pattern[j] == '?' || AreCharactersEqual(in pattern[j], in input[i], in comparisonType)))
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
}