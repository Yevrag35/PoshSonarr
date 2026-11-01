namespace MG.Sonarr.Next.Extensions.Strings;

/// <summary>
/// Provides extension methods for <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/> types.
/// </summary>
public static partial class CharSpanExtensions
{
	/// <summary>
	/// Copies the characters of a <see cref="ReadOnlySpan{T}"/> to a target <see cref="Span{T}"/>.
	/// </summary>
	/// <param name="values">The source span whose characters are to be copied.</param>
	/// <param name="destination">The target span where characters will be copied.</param>
	/// <param name="charsWritten">When this method returns, contains the number of characters written to the destination span.</param>
	/// <exception cref="ArgumentException"/>
	public static void CopyTo(this ReadOnlySpan<char> values, Span<char> destination, out int charsWritten)
	{
		values.CopyTo(destination);
		charsWritten = values.Length;
	}

	/// <summary>
	/// Copies the contents of the specified <see cref="ReadOnlySpan{T}"/> to a slice of the target <see cref="Span{T}"/>
	/// starting at the specified position.
	/// </summary>
	/// <param name="readOnlySpan">The source <see cref="ReadOnlySpan{T}"/> containing the data to copy.</param>
	/// <param name="span">The target <see cref="Span{T}"/> where the data will be copied.</param>
	/// <param name="currentPosition">The starting position in the target <paramref name="span"/> where the data will be copied.</param>
	/// <returns>The new position in the target <paramref name="span"/> after the copied data, calculated as the sum of <paramref
	/// name="currentPosition"/> and the length of <paramref name="readOnlySpan"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CopyToSlice(this ReadOnlySpan<char> readOnlySpan, Span<char> span, int currentPosition)
	{
		readOnlySpan.CopyTo(span.Slice(currentPosition));
		return readOnlySpan.Length + currentPosition;
	}

	/// <summary>
	/// Formats the specified value and writes it to a slice of the provided character span.
	/// </summary>
	/// <remarks>This method attempts to format the value using the <see cref="ISpanFormattable.TryFormat"/> method.
	/// If the formatting is successful, the <paramref name="currentPosition"/> is incremented by the number of characters
	/// written.</remarks>
	/// <typeparam name="T">The type of the value to format. Must implement <see cref="ISpanFormattable"/> and be non-nullable.</typeparam>
	/// <param name="value">The value to format and write to the span.</param>
	/// <param name="span">The span of characters to which the formatted value will be written.</param>
	/// <param name="currentPosition">The starting position within the span where the formatted value will be written.  This value is updated to reflect
	/// the position after the written content.</param>
	/// <param name="format">An optional read-only span of characters that specifies the format to apply to the value.  If not provided, the
	/// default format for the type is used.</param>
	/// <param name="provider">An optional <see cref="IFormatProvider"/> to use for culture-specific formatting.  If null, the current culture is
	/// used.</param>
	/// <returns>The updated position within the span after the formatted value has been written.</returns>
	public static int CopyToSlice<T>(this T value, Span<char> span, int currentPosition, ReadOnlySpan<char> format = default, IFormatProvider? provider = null) where T : notnull, ISpanFormattable
	{
		if (value.TryFormat(span.Slice(currentPosition), out int written, format, provider))
		{
			currentPosition += written;
		}

		return currentPosition;
	}

	/// <summary>
	/// Determines if the character at the specified index is escaped with the specified escape character.
	/// </summary>
	/// <param name="readOnlySpanValue">The span of characters where the indexed character occurs.</param>
	/// <param name="index">
	///     The index of the character within the span where the preceding characters will be checked.
	/// </param>
	/// <param name="escapeChar">
	///     The character that is marked as the escape character in the span. Defaults to a backslash <c>\</c>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the character at the specified index is found to be escaped;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	internal static bool IsEscapedAtUnsafe(this ReadOnlySpan<char> readOnlySpanValue, int index, char escapeChar = '\\')
	{
		ref char start = ref Unsafe.AsRef(in readOnlySpanValue.Ref());

		int i = index - 1;
		int escapeCount = 0;

		while (i >= 0 && start.Offset(i) == escapeChar)
		{
			escapeCount++;
			i--;
		}

		return (escapeCount & 1) != 0;
	}

	/// <summary>
	/// Determines if the character at the specified index of this <see cref="string"/> is escaped with
	/// the specified escape character.
	/// </summary>
	/// <param name="value">The string of characters where the indexed character occurs.</param>
	/// <param name="index">
	///     The index of the character within the span where the preceding characters will be check.
	/// </param>
	/// <param name="escapeChar">
	///     The character that is marked as the escape character in the span.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the character at the specified index is found to be escaped;
	/// otherwise, if it is not escaped or the <see cref="string"/> value is <see langword="null"/>, empty,
	/// or whitespace, <see langword="false"/>.
	/// </returns>
	public static bool IsEscapedAt(this ReadOnlySpan<char> value, int index, char escapeChar = '\\')
	{
		if (value.IsWhiteSpace() || index < 0 || index >= value.Length)
		{
			return false;
		}

		return value.IsEscapedAtUnsafe(index, escapeChar);
	}

	/// <summary>
	/// Copies the specified characters into the start of the target span.
	/// </summary>
	/// <remarks>The characters can be passed as one span or as separate characters, which the <see langword="params"/>
	/// span collects in the order given.</remarks>
	/// <param name="span">The target <see cref="Span{T}"/> where the characters will be copied.</param>
	/// <param name="value">The characters to copy.</param>
	/// <returns>The number of characters copied, which is the length of <paramref name="value"/>.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="span"/> is shorter than <paramref name="value"/>.</exception>
	public static int Push(this Span<char> span, params ReadOnlySpan<char> value)
	{
		value.CopyTo(span);
		return value.Length;
	}
}
