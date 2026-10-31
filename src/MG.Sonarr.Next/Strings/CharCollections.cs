using MG.Sonarr.Next.Buffers;

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
	public static SearchValues<byte> GetNumberValues(byte inclusiveStart, byte inclusiveEnd)
	{
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
	/// <param name="buffer">The destination buffer that receives lowercase alphabetic characters.</param>
	public static void WriteAlphaLowercase(Span<char> buffer)
	{
		CopyCharactersTo(buffer, 'a', 'z');
	}

	/// <summary>
	/// Writes the uppercase alphabet to the buffer.
	/// </summary>
	/// <param name="buffer">The destination buffer that receives uppercase alphabetic characters.</param>
	public static void WriteAlphaUppercase(Span<char> buffer)
	{
		CopyCharactersTo(buffer, 'A', 'Z');
	}

	/// <summary>
	/// Writes numeric characters to the buffer.
	/// </summary>
	/// <param name="buffer">The destination buffer that receives numeric characters.</param>
	public static void WriteNumbers(Span<char> buffer)
	{
		CopyCharactersTo(buffer, '0', '9');
	}

	/// <summary>
	/// Writes numeric ASCII byte values to the buffer.
	/// </summary>
	/// <param name="buffer">The destination buffer that receives numeric ASCII byte values.</param>
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
	private static void CopyCharactersTo(Span<byte> destination, char start, char end)
	{
		for (int i = 0; i < destination.Length; i++)
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
	private static void CopyCharactersTo(Span<char> destination, char start, char end)
	{
		for (int i = 0; i < destination.Length; i++)
		{
			destination[i] = (char)(start + i);
		}
	}
}
