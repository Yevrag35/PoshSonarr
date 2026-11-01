using MG.Sonarr.Next.Buffers;

namespace MG.Sonarr.Next.Strings;

public readonly partial struct Wildcard
{
	// SPAN PARSING
	/// <summary>
	/// Parses the provided read-only span of characters into a <see cref="Wildcard"/> pattern.
	/// </summary>
	/// <param name="s">The read-only span of characters to parse as a wildcard pattern.</param>
	/// <returns>
	/// A new <see cref="Wildcard"/> instance representing the parsed span of characters.
	/// If <paramref name="s"/> is empty, returns an empty <see cref="Wildcard"/> instance.
	/// </returns>
	public static Wildcard Parse(params ReadOnlySpan<char> s)
	{
		return s.Length switch
		{
			0 => Empty,
			1 when s[0] is '*' => All,
			_ => new(s),
		};
	}
	/// <summary>
	/// Parses the provided <see cref="string"/> instance in a <see cref="Wildcard"/> pattern.
	/// </summary>
	/// <param name="s">The <see cref="string"/> instance to parse.</param>
	/// <returns>
	/// A new <see cref="Wildcard"/> instance representing the parsed <paramref name="s"/>.
	/// If <paramref name="s"/> is <see langword="null"/> or empty, returns an empty <see cref="Wildcard"/> instance.
	/// Text of white space alone is an exact pattern, which matches only the same white space.
	/// </returns>
	[DebuggerStepThrough]
	public static Wildcard Parse(string? s)
	{
		return s?.Length switch
		{
			null or 0 => Empty,
			1 when s[0] is '*' => All,
			_ => new(s),
		};
	}
	/// <summary>
	/// Attempts to parse the provided read-only span of characters into a <see cref="Wildcard"/> pattern.
	/// </summary>
	/// <param name="s">
	/// The read-only span of characters to parse as a wildcard pattern.
	/// </param>
	/// <param name="result">
	/// When this method returns, contains the <see cref="Wildcard"/> instance equivalent to the provided span of characters
	/// or <see cref="Empty"/> if the span of characters was empty.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the span of characters was successfully parsed and <paramref name="result"/>
	/// is not empty; otherwise, <see langword="false"/>.
	/// </returns>
	public static bool TryParse(ReadOnlySpan<char> s, out Wildcard result)
	{
		result = Parse(s);
		return !result.IsEmpty;
	}

	/// <summary>
	/// Creates a <see cref="Wildcard"/> pattern of the specified match type from a span of characters.
	/// </summary>
	/// <remarks>
	///     <para>
	///         For <see cref="WildcardMatchType.Like"/>, <see cref="WildcardMatchType.StartsWith"/>,
	///         <see cref="WildcardMatchType.EndsWith"/>, and <see cref="WildcardMatchType.Exact"/>, leading and
	///         trailing <c>*</c> characters are trimmed from <paramref name="value"/>, and <c>*</c> is added where
	///         the match type needs one. A "like" pattern keeps the <c>?</c> and <c>*</c> characters between its
	///         other characters as wildcards, which no other match type can hold.
	///     </para>
	///     <para>
	///         When nothing is left of <paramref name="value"/> after the trim, <see cref="WildcardMatchType.Like"/>,
	///         <see cref="WildcardMatchType.StartsWith"/>, and <see cref="WildcardMatchType.EndsWith"/> return
	///         <see cref="All"/>, since such a pattern matches any input, and <see cref="WildcardMatchType.Exact"/>
	///         returns <see cref="Empty"/>.
	///     </para>
	///     <para>
	///         <see cref="WildcardMatchType.None"/> parses <paramref name="value"/> as
	///         <see cref="Parse(ReadOnlySpan{char})"/> does, and returns <see cref="Empty"/> when it's empty. Any
	///         other match type returns <see cref="Empty"/>.
	///     </para>
	/// </remarks>
	/// <param name="matchType">The kind of match that the pattern performs.</param>
	/// <param name="value">The characters that the pattern is built from.</param>
	/// <returns>
	///     A <see cref="Wildcard"/> instance that matches <paramref name="value"/> as <paramref name="matchType"/>
	///     specifies, <see cref="All"/> for <see cref="WildcardMatchType.All"/>, or <see cref="Empty"/>.
	/// </returns>
	/// <exception cref="ArgumentException">
	///     Thrown when <paramref name="matchType"/> is <see cref="WildcardMatchType.StartsWith"/>,
	///     <see cref="WildcardMatchType.EndsWith"/>, or <see cref="WildcardMatchType.Exact"/> and
	///     <paramref name="value"/> contains wildcard characters other than leading or trailing <c>*</c>
	///     characters, or when <paramref name="matchType"/> is <see cref="WildcardMatchType.All"/> and
	///     <paramref name="value"/> is anything but a single <c>*</c>.
	/// </exception>
	public static Wildcard ParseAs(WildcardMatchType matchType, params ReadOnlySpan<char> value)
	{
		switch (matchType)
		{
			case WildcardMatchType.None:
				return Parse(value);

			case WildcardMatchType.All when value.Length == 1 && '*' == value[0]:
				return All;

			case WildcardMatchType.All:
				throw new ArgumentException("An all pattern must only be a single '*' character.", nameof(value));

			case WildcardMatchType.Like:
			case WildcardMatchType.StartsWith:
			case WildcardMatchType.EndsWith:
			case WildcardMatchType.Exact:
				break;

			default:
				return Empty;
		}

		value = value.Trim('*');
		if (matchType != WildcardMatchType.Like && value.ContainsAny(s_wildcardChars))
		{
			throw new ArgumentException(matchType switch
			{
				WildcardMatchType.StartsWith => "A starts-with pattern cannot contain wildcard characters other than its trailing '*'.",
				WildcardMatchType.EndsWith => "An ends-with pattern cannot contain wildcard characters other than its leading '*'.",
				_ => "An exact pattern cannot contain wildcard characters.",
			}, nameof(value));
		}

		if (value.IsEmpty)
		{
			return matchType == WildcardMatchType.Exact ? Empty : All;
		}

		int length = value.Length + 2;
		using var builder = SpanStringBuilder.Create(
			length <= MAX_STACKALLOC
				? stackalloc char[length]
				: length);

		if (matchType is WildcardMatchType.Like or WildcardMatchType.EndsWith)
		{
			builder.Append('*');
		}

		builder.Append(value);
		if (matchType is WildcardMatchType.Like or WildcardMatchType.StartsWith)
		{
			builder.Append('*');
		}

		return new(builder.AsSpan(), matchType);
	}

	// ISPANPARSABLE IMPLEMENTATIONS
	/// <inheritdoc/>
	[DebuggerStepThrough]
	static Wildcard ISpanParsable<Wildcard>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s);
	}
	/// <inheritdoc/>
	[DebuggerStepThrough]
	static Wildcard IParsable<Wildcard>.Parse(string? s, IFormatProvider? provider)
	{
		return Parse(s.AsSpan());
	}
	/// <inheritdoc/>
	[DebuggerStepThrough]
	static bool ISpanParsable<Wildcard>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Wildcard result)
	{
		return TryParse(s, out result);
	}
	/// <inheritdoc/>
	[DebuggerStepThrough]
	static bool IParsable<Wildcard>.TryParse(string? s, IFormatProvider? provider, out Wildcard result)
	{
		return TryParse(s.AsSpan(), out result);
	}

	//UTF-8 PARSING
	/// <summary>Parses a span of UTF-8 characters into a <see cref="Wildcard"/> object.</summary>
	/// <remarks>
	/// The bytes decode as <see cref="Encoding.UTF8"/> decodes them, so a byte sequence that isn't valid UTF-8 becomes
	/// U+FFFD, the replacement character, and the pattern then follows <see cref="Parse(ReadOnlySpan{char})"/>.
	/// </remarks>
	/// <param name="utf8Text">The span of UTF-8 characters to parse.</param>
	/// <returns>
	/// The <see cref="Wildcard"/> result of parsing <paramref name="utf8Text" />, or <see cref="Empty"/> when it's empty.
	/// </returns>
	public static Wildcard Parse(ReadOnlySpan<byte> utf8Text)
	{
		if (utf8Text.IsEmpty)
		{
			return Empty;
		}

		int length = Encoding.UTF8.GetMaxCharCount(utf8Text.Length);

		using (var buffer = RentedBuffer.Rent<char>(
			length <= MAX_STACKALLOC
				? stackalloc char[length]
				: length))
		{
			int written = Encoding.UTF8.GetChars(utf8Text, buffer.Span);
			Wildcard result = Parse(buffer.Slice(0, written));

			return result;
		}
	}

	/// <summary>Tries to parse a span of UTF-8 characters into a <see cref="Wildcard"/> instance.</summary>
	/// <remarks>
	/// The bytes decode as <see cref="Parse(ReadOnlySpan{byte})"/> decodes them, so a byte sequence that isn't valid
	/// UTF-8 becomes U+FFFD rather than failing the parse.
	/// </remarks>
	/// <param name="utf8Text">The span of UTF-8 characters to parse.</param>
	/// <param name="result">
	///		When this method returns, contains the <see cref="Wildcard"/> instance parsed from <paramref name="utf8Text"/>,
	///		or <see cref="Empty"/> if <paramref name="utf8Text"/> was empty.
	/// </param>
	/// <returns>
	///		<see langword="true"/> if <paramref name="utf8Text" /> was successfully parsed and <paramref name="result"/>
	///		is not empty, as <see cref="TryParse(ReadOnlySpan{char}, out Wildcard)"/> returns; otherwise,
	///		<see langword="false"/>.
	/// </returns>
	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out Wildcard result)
	{
		result = Parse(utf8Text);
		return !result.IsEmpty;
	}

	// IUTF8_SPAN_PARSABLE IMPLEMENTATIONS
	/// <summary>Parses a span of UTF-8 characters into a <see cref="Wildcard"/> object.</summary>
	/// <param name="utf8Text">The span of UTF-8 characters to parse.</param>
	/// <param name="provider">An object that provides culture-specific formatting information about <paramref name="utf8Text" />.</param>
	/// <returns>The <see cref="Wildcard"/> result of parsing <paramref name="utf8Text" />.</returns>
	[DebuggerStepThrough]
	static Wildcard IUtf8SpanParsable<Wildcard>.Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return Parse(utf8Text);
	}
	/// <summary>Tries to parse a span of UTF-8 characters into a <see cref="Wildcard"/> instance.</summary>
	/// <param name="utf8Text">The span of UTF-8 characters to parse.</param>
	/// <param name="provider">An object that provides culture-specific formatting information about <paramref name="utf8Text" />.</param>
	/// <param name="result">
	///		When this method returns, contains the <see cref="Wildcard"/> instance parsed from <paramref name="utf8Text"/>,
	///		or <see cref="Empty"/> if <paramref name="utf8Text"/> was empty.
	/// </param>
	/// <returns>
	///		<see langword="true"/> if <paramref name="utf8Text" /> was successfully parsed and <paramref name="result"/>
	///		is not empty; otherwise, <see langword="false"/>.
	/// </returns>
	[DebuggerStepThrough]
	static bool IUtf8SpanParsable<Wildcard>.TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out Wildcard result)
	{
		return TryParse(utf8Text, out result);
	}
}