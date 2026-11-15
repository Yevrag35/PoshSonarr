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

	// MATCHING

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
	/// <see cref="Wildcard"/> object based on traditional wildcard pattern rules while using the specified
	/// string comparison rules
	/// </summary>
	/// <remarks>
	///     Traditional pattern matching includes:
	///     <para>
	///         <code>* - 0 or more of any character.<br/>
	///     ? - exactly 1 of any character.</code>
	///     </para>
	///     <para>
	///         A <c>*</c> in the pattern is a wildcard wherever it stands, so <c>M*H</c> matches <c>M*A*S*H</c>, and
	///         a <c>?</c> takes one UTF-16 code unit, so a character outside the Basic Multilingual Plane takes two.
	///     </para>
	///     <para>
	///         Under <see cref="StringComparison.Ordinal"/> and <see cref="StringComparison.OrdinalIgnoreCase"/>, the
	///         text between the wildcards compares as a span under <paramref name="comparisonType"/>, in every match
	///         type, so a pattern with a <c>?</c> or an inner <c>*</c> folds case as one with only a leading or trailing
	///         <c>*</c> does, a surrogate pair included.
	///     </para>
	///     <para>
	///         Under a culture-sensitive comparison, an exact pattern and one with only a leading or trailing <c>*</c>
	///         compare linguistically, as <see cref="MemoryExtensions.Equals(ReadOnlySpan{char}, ReadOnlySpan{char}, StringComparison)"/>,
	///         <see cref="MemoryExtensions.StartsWith(ReadOnlySpan{char}, ReadOnlySpan{char}, StringComparison)"/>, and
	///         <see cref="MemoryExtensions.EndsWith(ReadOnlySpan{char}, ReadOnlySpan{char}, StringComparison)"/> do. A
	///         pattern with a <c>?</c> or an inner <c>*</c> compares one character at a time, as the matcher of
	///         <see cref="System.Management.Automation.WildcardPattern"/> does: ordinally when the comparison is case-sensitive, and otherwise after
	///         lowercasing each character with the comparison's culture. That matcher also takes the patterns with only a
	///         leading <c>*</c>, which it doesn't compare linguistically, so <c>*</c> followed by U+00E9 matches <c>e</c>
	///         followed by U+0301, the combining acute accent, here and not there.
	///     </para>
	/// </remarks>
	/// <param name="input">The string to pattern match.</param>
	/// <param name="comparisonType">The comparison rules to use when comparing the pattern against the input.</param>
	/// <returns>
	///     <see langword="true"/> if <paramref name="input"/> matches the <see cref="Wildcard"/> pattern;
	///     otherwise, <see langword="false"/>.
	/// </returns>
	/// <exception cref="ArgumentException">
	///     Thrown when <paramref name="comparisonType"/> isn't a value that <see cref="StringComparison"/> defines.
	/// </exception>
	public readonly bool IsMatch(ReadOnlySpan<char> input, StringComparison comparisonType)
	{
		if ((uint)comparisonType > (uint)StringComparison.OrdinalIgnoreCase)
		{
			ThrowComparisonNotSupported(nameof(comparisonType));
		}

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

	// PRIVATE METHODS

	/// <summary>
	/// Determines whether a literal character of the pattern equals a character of the input under the specified
	/// comparison.
	/// </summary>
	/// <remarks>
	///     <see cref="StringComparison.OrdinalIgnoreCase"/> folds two ASCII characters inline and any other pair as
	///     <see cref="MemoryExtensions.Equals(ReadOnlySpan{char}, ReadOnlySpan{char}, StringComparison)"/> folds them. A
	///     culture-sensitive comparison that ignores case lowercases both with <paramref name="textInfo"/>, as the
	///     matcher of <see cref="System.Management.Automation.WildcardPattern"/> does, and every other comparison
	///     compares the two ordinally.
	/// </remarks>
	/// <param name="x">The pattern's character.</param>
	/// <param name="y">The input's character.</param>
	/// <param name="comparisonType">The comparison, which <see cref="IsMatch(ReadOnlySpan{char}, StringComparison)"/> has validated.</param>
	/// <param name="textInfo">
	///     The casing rules of a culture-sensitive comparison that ignores case, or <see langword="null"/> for any other
	///     comparison.
	/// </param>
	/// <returns><see langword="true"/> if the characters are equal; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool AreCharactersEqual(char x, char y, StringComparison comparisonType, TextInfo? textInfo)
	{
		if (x == y)
		{
			return true;
		}

		if (comparisonType != StringComparison.OrdinalIgnoreCase)
		{
			return textInfo is not null && textInfo.ToLower(x) == textInfo.ToLower(y);
		}

		if ((x | y) <= 0x7F)
		{
			uint lower = (uint)(x | 0x20);
			return lower == (uint)(y | 0x20) && lower - 'a' <= 'z' - 'a';
		}

		return AreNonAsciiEqualIgnoreCase(x, y);
	}
	/// <summary>
	/// Determines whether two characters, at least one of them outside ASCII, are equal under
	/// <see cref="StringComparison.OrdinalIgnoreCase"/>.
	/// </summary>
	/// <param name="x">The first character.</param>
	/// <param name="y">The second character.</param>
	/// <returns><see langword="true"/> if the characters are equal; otherwise, <see langword="false"/>.</returns>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool AreNonAsciiEqualIgnoreCase(char x, char y)
	{
		return new ReadOnlySpan<char>(in x).Equals(new ReadOnlySpan<char>(in y), StringComparison.OrdinalIgnoreCase);
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
	private static WildcardMatchType DeterminePattern(string? pattern)
	{
		return pattern is null ? WildcardMatchType.None : DeterminePattern(pattern.AsSpan());
	}
	private static WildcardMatchType DeterminePattern(ReadOnlySpan<char> pattern)
	{
		return pattern.IsEmpty ? WildcardMatchType.None : determineFromNonEmpty(pattern);

		static WildcardMatchType determineFromNonEmpty(ReadOnlySpan<char> pattern)
		{
			if (TryAsSingleCharacterPattern(pattern, out WildcardMatchType matchType))
			{
				return matchType;
			}

			if (!pattern.ContainsAny(s_wildcardChars))
			{
				return WildcardMatchType.Exact;
			}

			matchType = WildcardMatchType.Like;
			if ('*' == pattern[^1] && !pattern.TrimEnd('*').ContainsAny(s_wildcardChars))
			{
				matchType = WildcardMatchType.StartsWith;
			}
			else if ('*' == pattern[0] && !pattern.TrimStart('*').ContainsAny(s_wildcardChars))
			{
				matchType = WildcardMatchType.EndsWith;
			}

			return matchType;
		}
	}
	/// <summary>
	/// Determines whether the input matches a pattern that holds a <c>?</c> or a <c>*</c> between its other characters.
	/// </summary>
	/// <remarks>
	///     A <c>*</c> in the pattern takes any number of characters, checked before the input's character is, so a
	///     <c>*</c> in the input never stands in for one in the pattern. A <c>?</c> takes one UTF-16 code unit, and each
	///     other character has to equal the input's, compared by <see cref="AreCharactersEqual"/>, except that under
	///     <see cref="StringComparison.OrdinalIgnoreCase"/> a surrogate pair in the pattern compares with the input's
	///     next two characters as one character, so it folds as it does in a whole string. On a mismatch, the most
	///     recent <c>*</c> takes one more character and matching resumes after it, which is enough, since a later
	///     <c>*</c> can always take what an earlier one would.
	/// </remarks>
	/// <param name="pattern">The pattern.</param>
	/// <param name="input">The text to match.</param>
	/// <param name="comparisonType">The comparison, which <see cref="IsMatch(ReadOnlySpan{char}, StringComparison)"/> has validated.</param>
	/// <returns><see langword="true"/> if <paramref name="input"/> matches <paramref name="pattern"/>; otherwise, <see langword="false"/>.</returns>
	private static bool IsMatch(ReadOnlySpan<char> pattern, ReadOnlySpan<char> input, StringComparison comparisonType)
	{
		TextInfo? textInfo = comparisonType switch
		{
			StringComparison.CurrentCultureIgnoreCase => CultureInfo.CurrentCulture.TextInfo,
			StringComparison.InvariantCultureIgnoreCase => CultureInfo.InvariantCulture.TextInfo,
			_ => null,
		};

		int starIndex = -1;
		int resumeIndex = 0;

		int i = 0;
		int j = 0;

		while ((uint)i < (uint)input.Length)
		{
			if ((uint)j < (uint)pattern.Length)
			{
				char p = pattern[j];
				if (p == '*')
				{
					starIndex = j;
					resumeIndex = i;
					j++;
					continue;
				}

				if (p == '?')
				{
					i++;
					j++;
					continue;
				}

				if (char.IsHighSurrogate(p)
					&& comparisonType == StringComparison.OrdinalIgnoreCase
					&& (uint)(j + 1) < (uint)pattern.Length
					&& char.IsLowSurrogate(pattern[j + 1]))
				{
					unsafe
					{
						// SAFETY: i and j start at 0 and only grow, the check above keeps j + 1 within pattern, and the first
						// condition here keeps i + 1 within input, so each two-character slice lies within its span.
						if ((uint)(i + 1) < (uint)input.Length
							&& pattern.UnsafeSlice(j, 2).Equals(input.UnsafeSlice(i, 2), StringComparison.OrdinalIgnoreCase))
						{
							i += 2;
							j += 2;
							continue;
						}
					}
				}
				else if (AreCharactersEqual(p, input[i], comparisonType, textInfo))
				{
					i++;
					j++;
					continue;
				}
			}

			if (starIndex < 0)
			{
				return false;
			}

			j = starIndex + 1;
			i = ++resumeIndex;
		}

		while ((uint)j < (uint)pattern.Length && pattern[j] == '*')
		{
			j++;
		}

		return j == pattern.Length;
	}
	/// <summary>
	/// Throws the <see cref="ArgumentException"/> for a <see cref="StringComparison"/> value that the enum doesn't
	/// define, with the message that the string comparisons of .NET give it.
	/// </summary>
	/// <param name="paramName">The name of the parameter that holds the value.</param>
	/// <exception cref="ArgumentException">Thrown on every call.</exception>
	[DoesNotReturn, StackTraceHidden]
	private static void ThrowComparisonNotSupported(string? paramName)
	{
		throw new ArgumentException("The string comparison type passed in is currently not supported.", paramName);
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
}