namespace MG.Sonarr.Next.Models;

/// <summary>
/// Represents text that's cut short after a word and ended with three periods, such as a series' short overview.
/// </summary>
public readonly struct ShortOverview
{
	/// <summary>
	/// The largest buffer, in characters, that <see cref="Truncate(ReadOnlySpan{char}, int)"/> allocates on the stack
	/// before it rents an array instead.
	/// </summary>
	const int MAX_SPAN_LENGTH = 1_000;
	/// <summary>
	/// A value that indicates whether <see cref="_text"/> holds anything but white space.
	/// </summary>
	readonly bool _isNotEmpty;
	/// <summary>
	/// The shortened text, or <see langword="null"/> for a default instance.
	/// </summary>
	readonly string? _text;

	/// <summary>
	/// Gets a value that indicates whether the shortened text is empty.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the text is empty or consists only of white space, or the instance is the default
	/// one; otherwise, <see langword="false"/>.
	/// </value>
	public bool IsEmpty => !_isNotEmpty;
	/// <summary>
	/// Gets the shortened text.
	/// </summary>
	/// <value>The shortened text, or <see cref="string.Empty"/> for a default instance.</value>
	public string Text => _text ?? string.Empty;

	/// <summary>
	/// Initializes a new <see cref="ShortOverview"/> instance that holds the specified shortened text.
	/// </summary>
	/// <param name="text">The text, already shortened.</param>
	private ShortOverview(string text)
	{
		_text = text;
		_isNotEmpty = !string.IsNullOrWhiteSpace(text);
	}

	/// <summary>
	/// Creates a <see cref="ShortOverview"/> from text, cut after the word that the specified length ends in.
	/// </summary>
	/// <remarks>
	/// The text keeps its first <paramref name="howLong"/> characters and the rest of the word that they end in, and
	/// then ends with three periods, with white space trimmed from both ends.
	/// </remarks>
	/// <param name="text">The text to shorten.</param>
	/// <param name="howLong">
	/// The number of characters to keep before looking for the end of a word, from 1 through the length of
	/// <paramref name="text"/>.
	/// </param>
	/// <returns>The shortened text.</returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when <paramref name="howLong"/> is less than 1 or greater than the length of <paramref name="text"/>.
	/// </exception>
	public static ShortOverview FromValue(ReadOnlySpan<char> text, int howLong)
	{
		string shorted = Truncate(text, howLong);
		return new ShortOverview(shorted);
	}

	/// <summary>
	/// Cuts text after the word that the specified length ends in and ends it with three periods.
	/// </summary>
	/// <remarks>
	/// The work is done in a buffer of the text's length plus three, on the stack for up to
	/// <see cref="MAX_SPAN_LENGTH"/> characters and in an array rented from <see cref="ArrayPool{T}.Shared"/> beyond
	/// that, which goes back to the pool before the method returns.
	/// </remarks>
	/// <param name="text">The text to shorten.</param>
	/// <param name="shortenedLength">The number of characters to keep before looking for the end of a word.</param>
	/// <returns>The shortened text, with white space trimmed from both ends.</returns>
	private static string Truncate(ReadOnlySpan<char> text, int shortenedLength)
	{
		bool isRented = false;
		char[]? array = null;
		Span<char> threeDots = stackalloc char[3];
		threeDots.Fill('.');

		int useLength = text.Length + threeDots.Length;

		Span<char> span = useLength <= MAX_SPAN_LENGTH
			? stackalloc char[useLength]
			: RentArray(useLength, ref isRented, ref array);

		text.Slice(0, shortenedLength).CopyTo(span);
		int position = shortenedLength;

		while (!char.IsWhiteSpace(span[position - 1]) && position < text.Length)
		{
			int current = position;
			span[current] = text[current];
			position++;
		}

		if (!char.IsWhiteSpace(span[position - 1]))
		{
			position++;
		}

		threeDots.CopyTo(span.Slice(position - 1));
		position += threeDots.Length - 1;

		string s = new(span.Slice(0, position).Trim());
		if (isRented)
		{
			ArrayPool<char>.Shared.Return(array!);
		}

		return s;
	}
	/// <summary>
	/// Rents an array from <see cref="ArrayPool{T}.Shared"/> and records that it's rented.
	/// </summary>
	/// <param name="length">The number of characters that the buffer needs.</param>
	/// <param name="isRented">Set to <see langword="true"/>.</param>
	/// <param name="array">Set to the rented array, which the caller returns to the pool.</param>
	/// <returns>A span over the first <paramref name="length"/> elements of the rented array.</returns>
	private static Span<char> RentArray(int length, ref bool isRented, ref char[]? array)
	{
		array = ArrayPool<char>.Shared.Rent(length);
		isRented = true;
		return array.AsSpan(0, length);
	}
}

