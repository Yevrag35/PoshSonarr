using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Extensions.Strings;
using System.Text;

namespace MG.Sonarr.Next.Strings;
public readonly partial struct Wildcard
{
	#region SPAN PARSING
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
			1 when s[0] is '*' or '%' => All,
			_ => new(s),
		};
	}
	/// <summary>
	/// Parses the provided <see cref="string"/> instance in a <see cref="Wildcard"/> pattern.
	/// </summary>
	/// <param name="s">The <see cref="string"/> instance to parse.</param>
	/// <returns>
	/// A new <see cref="Wildcard"/> instance representing the parsed <paramref name="s"/>.
	/// If <paramref name="s"/> is <see langword="null"/>, empty, or whitespace, returns an empty <see cref="Wildcard"/> instance.
	/// </returns>
	[DebuggerStepThrough]
	public static Wildcard Parse(string? s)
	{
		return s?.Length switch
		{
			null or 0 => Empty,
			1 when s[0] is '*' or '%' => All,
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

	public static Wildcard ParseAs(WildcardMatchType matchType, params ReadOnlySpan<char> value)
	{
		Span<char> chars = stackalloc char[value.Length + 2];
		int pos = 0;
		switch (matchType)
		{
            case WildcardMatchType.None when !value.IsEmpty:
				return Parse(value);

            case WildcardMatchType.Like:
				value = value.Trim('*');
				chars[pos++] = '*';
				value.CopyToSlice(chars, ref pos);
                chars[pos++] = '*';
				break;

			case WildcardMatchType.StartsWith:
				value = value.Trim('*');
                value.CopyToSlice(chars, ref pos);
				chars[pos++] = '*';
				break;

			case WildcardMatchType.EndsWith:
				value = value.Trim('*');
                chars[pos++] = '*';
                value.CopyToSlice(chars, ref pos);
				break;

			case WildcardMatchType.Exact when value.Trim('*').ContainsAny(_wildcardChars):
				throw new ArgumentException("An exact pattern cannot contain wildcard characters.", nameof(value));

			case WildcardMatchType.Exact:
				value = value.Trim('*');
                value.CopyToSlice(chars, ref pos);
				break;

            case WildcardMatchType.All when value.Length == 1 && '*' == value[0]:
                return All;

			case WildcardMatchType.All:
				throw new ArgumentException("An all pattern must only be a single '*' character.", nameof(value));

			default:
				return Empty;
        }

        return new(chars.Slice(0, pos), matchType);
    }

	#region I_SPAN_PARSABLE IMPLEMENTATIONS
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

	#endregion

	#endregion

	#region UTF8 PARSING

	/// <summary>Parses a span of UTF-8 characters into a <see cref="Wildcard"/> object.</summary>
	/// <param name="utf8Text">The span of UTF-8 characters to parse.</param>
	/// <returns>The <see cref="Wildcard"/> result of parsing <paramref name="utf8Text" />.</returns>
	public static Wildcard Parse(ReadOnlySpan<byte> utf8Text)
	{
		if (utf8Text.IsEmpty)
		{
			return Empty;
		}

		int length = Encoding.UTF8.GetMaxCharCount(utf8Text.Length);

		RentedBuffer<char> array = [];

		Span<char> buffer = length <= MAX_STACKALLOC
			? stackalloc char[MAX_STACKALLOC]
			: RentedBuffer.Rent(length, ref array);

		int written = Encoding.UTF8.GetChars(utf8Text, buffer);
		Wildcard result = Parse(buffer.Slice(0, written));

		array.Dispose();

		return result;
	}

	/// <summary>Tries to parse a span of UTF-8 characters into a <see cref="Wildcard"/> instance.</summary>
	/// <param name="utf8Text">The span of UTF-8 characters to parse.</param>
	/// <param name="result">On return, contains the result of successfully parsing <paramref name="utf8Text" /> or an undefined value on failure.</param>
	/// <returns>
	///		<see langword="true"/> if <paramref name="utf8Text" /> was successfully parsed; 
	///		otherwise, <see langword="false"/>.
	/// </returns>
	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out Wildcard result)
	{
		result = Parse(utf8Text);
		return result.IsEmpty == utf8Text.IsEmpty;
	}

	#region I_UTF8_SPAN_PARSABLE IMPLEMENTATIONS
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
	/// <param name="result">On return, contains the result of successfully parsing <paramref name="utf8Text" /> or an undefined value on failure.</param>
	/// <returns>
	///		<see langword="true"/> if <paramref name="utf8Text" /> was successfully parsed; 
	///		otherwise, <see langword="false"/>.
	/// </returns>
	[DebuggerStepThrough]
	static bool IUtf8SpanParsable<Wildcard>.TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out Wildcard result)
	{
		return TryParse(utf8Text, out result);
	}

	#endregion

	#endregion
}