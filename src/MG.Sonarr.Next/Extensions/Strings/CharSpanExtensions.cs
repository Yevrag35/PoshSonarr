using MG.Sonarr.Next.Extensions.Strings;
using System.Buffers;

namespace MG.Sonarr.Next.Extensions.Strings;

/// <summary>
/// Custom extension methods for <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/> instances
/// of type <see cref="char"/>.
/// </summary>
public static partial class CharSpanExtensions
{
	/// <summary>
	/// Copies the contents of this <see cref="ReadOnlySpan{T}"/> into a destination 
	/// <see cref="Span{T}"/> and advances the given ref <see cref="int"/> the number 
	/// of characters that were copied.
	/// </summary>
	/// <param name="spanValue">The source read-only span whose characters are copied.</param>
	/// <param name="span">The span to copy items into.</param>
	/// <param name="position">
	///     The ref <see cref="int"/> to add the number of the characters to if copying was
	///     successful.
	/// </param>
	[Obsolete("Use the overload that returns int.")]
	public static void CopyToSlice(this ReadOnlySpan<char> spanValue, Span<char> span, ref int position)
	{
		if (spanValue.TryCopyTo(span.Slice(position)))
		{
			position += spanValue.Length;
		}
	}
	[Obsolete("Use the overload that returns int.")]
	public static void CopyToSlice<T>(this T value, Span<char> destination, ref int position, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
		where T : ISpanFormattable
	{
		if (value.TryFormat(destination.Slice(position), out int written, format, provider))
		{
			position += written;
		}
	}
	/// <summary>
	/// Determines whether the specified span is enclosed by the given opening and closing characters.
	/// </summary>
	/// <param name="readOnlySpan">The span of characters to examine for enclosure.</param>
	/// <param name="openingChar">The character that should appear at the start of the span.</param>
	/// <param name="closingChar">The character that should appear at the end of the span.</param>
	/// <returns><see langword="true"/> if the span begins with the opening character and ends with the closing character; otherwise,
	/// <see langword="false"/>.</returns>
	public static bool EnclosedIn(this ReadOnlySpan<char> readOnlySpan, char openingChar, char closingChar)
	{
		return EnclosedInCore(readOnlySpan, openingChar, closingChar);
	}
	/// <summary>
	/// Copies characters from the source span to the destination span, omitting any characters that match those
	/// specified in the <paramref name="removeChars"/> set.
	/// </summary>
	/// <remarks>If the destination span is not large enough to hold all non-removed characters, only
	/// as many characters as will fit are written. The method does not throw an exception in this case; excess
	/// characters are omitted. The operation does not allocate additional memory and is suitable for
	/// performance-critical scenarios.</remarks>
	/// <param name="source">The read-only span of characters to process and copy from.</param>
	/// <param name="destination">The span of characters to which the filtered result will be written. Must be large enough to hold all
	/// non-removed characters.</param>
	/// <param name="removeChars">A set of characters to remove from the source span during copying. Any character in this set will be
	/// excluded from the destination.</param>
	/// <returns>The number of characters written to the destination span after removal of specified characters.</returns>
	public static int RemoveAny(this ReadOnlySpan<char> source, Span<char> destination, SearchValues<char> removeChars)
	{
		int written = 0;
		ReadOnlySpan<char> slice = source;

		while (!slice.IsEmpty)
		{
			int index = slice.IndexOfAny(removeChars);
			if (index == -1)
			{
				// No more matches, copy the remaining portion and exit.
				written = slice.CopyToSlice(destination, written);
				break;
			}

			if (index == 0)
			{
				slice = slice.Slice(1);
				continue;
			}

			written = slice.Slice(0, index).CopyToSlice(destination, written);
			if (index < slice.Length - 1)
			{
				slice = slice.Slice(index + 1);
			}
			else
			{
				slice = [];
			}
		}

		return written;
	}

	/// <summary>
	/// Attemps to copy the contents of this <see cref="ReadOnlySpan{T}"/> into a 
	/// <see cref="Span{T}"/> and advancing the given ref <see cref="int"/> the number 
	/// of characters that were copied, returning a value indicating whether or not the operation
	/// succeeded.
	/// </summary>
	/// <param name="spanValue">The source read-only span whose characters are copied.</param>
	/// <param name="span">The target of the copy operation.</param>
	/// <param name="position">
	///     The ref <see cref="int"/> to add the number of the characters to if copying was
	///     successful.
	/// </param>
	/// <returns>
	///     <see langword="true"/> if the copying operation was successful; otherwise
	///     <see langword="false"/>.
	/// </returns>
	public static bool TryCopyToSlice(this ReadOnlySpan<char> spanValue, Span<char> span, ref int position)
	{
		bool result = false;

		if (spanValue.TryCopyTo(span.Slice(position)))
		{
			position += spanValue.Length;
			result = true;
		}

		return result;
	}

	public static bool TryCopyToSlice<T>(this T value, Span<char> destination, ref int position, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
		where T : ISpanFormattable
	{
		bool result = false;
		if (value.TryFormat(destination.Slice(position), out int written, format, provider))
		{
			position += written;
			result = true;
		}

		return result;
	}
	/// <summary>
	/// Determines whether the beginning of the <paramref name="readOnlySpan"/> matches the specified <paramref name="value"/> when compared ignoring case.
	/// </summary>
	/// <param name="readOnlySpan">The source span.</param>
	/// <param name="value">The character to compare to the beginning of the source span.</param>
	/// <returns>
	///     <see langword="true"/> if <paramref name="value"/> matches the beginning of 
	///     <paramref name="readOnlySpan"/>; otherwise, <see langword="false"/>.
	/// </returns>
	[DebuggerStepThrough]
	public static bool StartsWith(this ReadOnlySpan<char> readOnlySpan, char value)
	{
		return FirstCharEquals(readOnlySpan, value);
	}
	/// <summary>
	/// Determines whether the beginning of the <paramref name="readOnlySpan"/> matches the specified <paramref name="value"/> when compared using the specified 
	/// <paramref name="comparisonType"/> option.
	/// </summary>
	/// <param name="readOnlySpan">The source span.</param>
	/// <param name="value">The character to compare to the beginning of the source span.</param>
	/// <param name="comparisonType">
	///     One of the enumeration values that determines how the 
	///     <paramref name="readOnlySpan"/> and <paramref name="value"/> are compared.
	/// </param>
	/// <returns>
	///     <see langword="true"/> if <paramref name="value"/> matches the beginning of 
	///     <paramref name="readOnlySpan"/>; otherwise, <see langword="false"/>.
	/// </returns>
	[DebuggerStepThrough]
	public static bool StartsWith(this ReadOnlySpan<char> readOnlySpan, char value, StringComparison comparisonType)
	{
		return readOnlySpan.StartsWith(new ReadOnlySpan<char>(in value), comparisonType);
	}

	/// <summary>
	/// Attempts to find the last occurrence of a specified substring within the current span using the specified string
	/// comparison.
	/// </summary>
	/// <param name="chars">The span of characters to search within.</param>
	/// <param name="value">The substring to locate within the span.</param>
	/// <param name="comparisonType">The type of string comparison to use when searching for the substring.</param>
	/// <param name="index">When this method returns, contains the zero-based index of the last occurrence of <paramref name="value"/> within
	/// <paramref name="chars"/>, if found; otherwise, -1.</param>
	/// <returns><see langword="true"/> if the substring is found within the span; otherwise, <see langword="false"/>.</returns>
	public static bool TryLastIndexOf(this ReadOnlySpan<char> chars, [ConstantExpected] string value, StringComparison comparisonType, out int index)
	{
		index = chars.LastIndexOf(value, comparisonType);
		return index != -1;
	}

	#region PRIVATE METHODS
	private static bool EnclosedInCore(ReadOnlySpan<char> readOnlySpan, char opening, char closing)
	{
		if (readOnlySpan.Length < 2)
		{
			return false;
		}

		return opening == readOnlySpan[0] && closing == readOnlySpan[^1];
	}

	private static bool FirstCharEquals(ReadOnlySpan<char> span, char value)
	{
		bool result = false;
		if (!span.IsEmpty)
		{
			result = span[0] == value;
		}

		return result;
	}

	#endregion
}