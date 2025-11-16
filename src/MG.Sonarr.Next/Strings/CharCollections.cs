using MG.Sonarr.Next.Buffers;
using System.Buffers;

namespace MG.Sonarr.Next.Strings;

public static class CharCollections
{
	public static readonly SearchValues<char> AllAlphaNumeric;
	public static readonly SearchValues<char> AlphaLowercase;
	public static readonly SearchValues<char> AlphaUppercase;
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

	public static SearchValues<byte> GetNumberValues(byte inclusiveStart, byte inclusiveEnd)
	{
		int length = inclusiveEnd - inclusiveStart + 1;

		RentedBuffer<byte> buffer = [];
		Span<byte> bytes = length <= MAX_STACKALLOC
			? stackalloc byte[length]
			: RentedBuffer.Rent<byte>(length, ref buffer);

		for (int i = inclusiveStart; i <= inclusiveEnd; i++)
		{
			buffer[i - inclusiveStart] = (byte)i;
		}

		SearchValues<byte> values = SearchValues.Create(buffer.Buffer);

		buffer.Dispose();
		return values;
	}
	/// <summary>
	/// Writes the lowercase alphabet to the buffer.
	/// </summary>
	/// <param name="buffer"></param>
	/// <exception cref="ArgumentException">The buffer is too small.</exception>
	public static void WriteAlphaLowercase(Span<char> buffer)
	{
		CopyCharactersTo(buffer, 'a', 'z');
	}
	public static void WriteAlphaUppercase(Span<char> buffer)
	{
		CopyCharactersTo(buffer, 'A', 'Z');
	}
	public static void WriteNumbers(Span<char> buffer)
	{
		CopyCharactersTo(buffer, '0', '9');
	}
	public static void WriteNumbers(Span<byte> buffer)
	{
		CopyCharactersTo(buffer, '0', '9');
	}

	private static void CopyCharactersTo(Span<byte> destination, char start, char end)
	{
		for (int i = 0; i < destination.Length; i++)
		{
			destination[i] = (byte)(start + i);
		}
	}

	private static void CopyCharactersTo(Span<char> destination, char start, char end)
	{
		for (int i = 0; i < destination.Length; i++)
		{
			destination[i] = (char)(start + i);
		}
	}
}
