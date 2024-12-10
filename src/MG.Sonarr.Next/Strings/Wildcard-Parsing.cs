using MG.Sonarr.Next.Components;
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
		if (s.IsEmpty)
		{
			return Empty;
		}

		if (s.Length == 1)
		{
			ref readonly char c = ref s[0];
			if ('*' == c || '%' == c)
			{
				return All;
			}
		}

		return new(s);
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
		return Parse(s.AsSpan());
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