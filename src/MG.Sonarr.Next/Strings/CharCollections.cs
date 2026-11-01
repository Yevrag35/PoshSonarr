using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Resources;

namespace MG.Sonarr.Next.Strings;

/// <summary>
/// Provides cached character sets and helpers for alphabetic and numeric ranges.
/// </summary>
public static class CharCollections
{
	/// <summary>
	/// Represents search values that include uppercase letters, lowercase letters, and numeric characters.
	/// </summary>
	public static readonly SearchValues<char> AllAlphaNumeric;

	/// <summary>
	/// Represents search values for lowercase alphabetic characters.
	/// </summary>
	public static readonly SearchValues<char> AlphaLowercase;

	/// <summary>
	/// Represents search values for uppercase alphabetic characters.
	/// </summary>
	public static readonly SearchValues<char> AlphaUppercase;

	/// <summary>
	/// Represents search values for numeric characters.
	/// </summary>
	public static readonly SearchValues<char> Numbers;
	private const int ALPHA_LENGTH = 26;
	private const int MAX_STACKALLOC = 256;

	static CharCollections()
	{
		Span<char> buffer = stackalloc char[ALPHA_LENGTH * 2 + 10];
		WriteAlphaLowercase(buffer.Slice(0, ALPHA_LENGTH));
		WriteAlphaUppercase(buffer.Slice(ALPHA_LENGTH, ALPHA_LENGTH));
		WriteNumbers(buffer.Slice(ALPHA_LENGTH * 2, 10));

		AlphaLowercase = SearchValues.Create(buffer.Slice(0, ALPHA_LENGTH));
		AlphaUppercase = SearchValues.Create(buffer.Slice(ALPHA_LENGTH, ALPHA_LENGTH));
		Numbers = SearchValues.Create(buffer.Slice(ALPHA_LENGTH * 2, 10));
		AllAlphaNumeric = SearchValues.Create(buffer);
	}

	/// <summary>
	/// Gets a <see cref="SearchValues{T}"/> instance for the specified inclusive byte range.
	/// </summary>
	/// <param name="inclusiveStart">The first byte value in the range.</param>
	/// <param name="inclusiveEnd">The last byte value in the range.</param>
	/// <returns>A <see cref="SearchValues{T}"/> instance containing all byte values in the inclusive range.</returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="inclusiveEnd"/> is less than <paramref name="inclusiveStart"/>.
	/// </exception>
	public static SearchValues<byte> GetNumberValues(byte inclusiveStart, byte inclusiveEnd)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(inclusiveEnd, inclusiveStart);
		int length = inclusiveEnd - inclusiveStart + 1;

		using (var buffer = RentedBuffer.Rent<byte>(
			length <= MAX_STACKALLOC
				? stackalloc byte[length]
				: length))
		{
			for (int i = inclusiveStart; i <= inclusiveEnd; i++)
			{
				buffer[i - inclusiveStart] = (byte)i;
			}

			SearchValues<byte> values = SearchValues.Create(buffer.Span);

			return values;
		}
	}
	/// <summary>
	/// Writes the lowercase alphabet to the buffer.
	/// </summary>
	/// <remarks>
	/// The 26 letters, <c>a</c> to <c>z</c>, go to the start of <paramref name="buffer"/>, and any characters after
	/// them stay as they were.
	/// </remarks>
	/// <param name="buffer">The destination buffer that receives lowercase alphabetic characters.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="buffer"/> is shorter than 26 characters.</exception>
	public static void WriteAlphaLowercase(Span<char> buffer)
	{
		CopyCharactersTo(buffer, 'a', 'z');
	}

	/// <summary>
	/// Writes the uppercase alphabet to the buffer.
	/// </summary>
	/// <remarks>
	/// The 26 letters, <c>A</c> to <c>Z</c>, go to the start of <paramref name="buffer"/>, and any characters after
	/// them stay as they were.
	/// </remarks>
	/// <param name="buffer">The destination buffer that receives uppercase alphabetic characters.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="buffer"/> is shorter than 26 characters.</exception>
	public static void WriteAlphaUppercase(Span<char> buffer)
	{
		CopyCharactersTo(buffer, 'A', 'Z');
	}

	/// <summary>
	/// Writes numeric characters to the buffer.
	/// </summary>
	/// <remarks>
	/// The 10 digits, <c>0</c> to <c>9</c>, go to the start of <paramref name="buffer"/>, and any characters after
	/// them stay as they were.
	/// </remarks>
	/// <param name="buffer">The destination buffer that receives numeric characters.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="buffer"/> is shorter than 10 characters.</exception>
	public static void WriteNumbers(Span<char> buffer)
	{
		CopyCharactersTo(buffer, '0', '9');
	}

	/// <summary>
	/// Writes numeric ASCII byte values to the buffer.
	/// </summary>
	/// <remarks>
	/// The 10 ASCII digits, <c>0x30</c> to <c>0x39</c>, go to the start of <paramref name="buffer"/>, and any bytes
	/// after them stay as they were.
	/// </remarks>
	/// <param name="buffer">The destination buffer that receives numeric ASCII byte values.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="buffer"/> is shorter than 10 bytes.</exception>
	public static void WriteNumbers(Span<byte> buffer)
	{
		CopyCharactersTo(buffer, '0', '9');
	}

	/// <summary>
	/// Copies sequential character byte values into the destination span.
	/// </summary>
	/// <param name="destination">The destination span that receives converted character bytes.</param>
	/// <param name="start">The first character in the sequence.</param>
	/// <param name="end">The last character in the sequence.</param>
	/// <param name="paramName">The name of the public method's parameter that <paramref name="destination"/> came from.</param>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="destination"/> is shorter than the sequence from <paramref name="start"/> to
	/// <paramref name="end"/>.
	/// </exception>
	private static void CopyCharactersTo(Span<byte> destination, char start, char end, [CallerArgumentExpression(nameof(destination))] string? paramName = null)
	{
		int length = end - start + 1;
		ThrowIfTooSmall(destination.Length, length, paramName);
		for (int i = 0; i < length; i++)
		{
			destination[i] = (byte)(start + i);
		}
	}

	/// <summary>
	/// Copies sequential characters into the destination span.
	/// </summary>
	/// <param name="destination">The destination span that receives characters.</param>
	/// <param name="start">The first character in the sequence.</param>
	/// <param name="end">The last character in the sequence.</param>
	/// <param name="paramName">The name of the public method's parameter that <paramref name="destination"/> came from.</param>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="destination"/> is shorter than the sequence from <paramref name="start"/> to
	/// <paramref name="end"/>.
	/// </exception>
	private static void CopyCharactersTo(Span<char> destination, char start, char end, [CallerArgumentExpression(nameof(destination))] string? paramName = null)
	{
		int length = end - start + 1;
		ThrowIfTooSmall(destination.Length, length, paramName);
		for (int i = 0; i < length; i++)
		{
			destination[i] = (char)(start + i);
		}
	}

	/// <summary>
	/// Throws when a destination is too short for the sequence that's copied into it.
	/// </summary>
	/// <param name="destinationLength">The destination's length.</param>
	/// <param name="length">The sequence's length.</param>
	/// <param name="paramName">The name of the parameter that holds the destination.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="destinationLength"/> is less than <paramref name="length"/>.</exception>
	private static void ThrowIfTooSmall(int destinationLength, int length, string? paramName)
	{
		if (destinationLength < length)
		{
			throw new ArgumentException(
				string.Format(CultureInfo.CurrentCulture, ExMessages.Buffer_TooSmall, length, destinationLength),
				paramName);
		}
	}
}
