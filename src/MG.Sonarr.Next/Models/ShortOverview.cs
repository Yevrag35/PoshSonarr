namespace MG.Sonarr.Next.Models;

/// <summary>
/// Represents text that's cut short after a word and ended with three periods, such as a series' short overview.
/// </summary>
public readonly struct ShortOverview
{
	/// <summary>
	/// The three periods that end the shortened text.
	/// </summary>
	//const string ELLIPSIS = "...";
	private static ReadOnlySpan<char> _ellipsis => ['.', '.', '.'];

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
	[MemberNotNullWhen(false, nameof(_text))]
	public bool IsEmpty => string.IsNullOrEmpty(_text);
	/// <summary>
	/// Gets the shortened text.
	/// </summary>
	/// <value>The shortened text, or <see cref="string.Empty"/> for a default instance.</value>
	public string Text => _text ?? string.Empty;

	/// <summary>
	/// Initializes a new <see cref="ShortOverview"/> instance that holds the specified shortened text.
	/// </summary>
	/// <param name="text">The text, already shortened.</param>
	private ShortOverview(string? text)
	{
		_text = text ?? string.Empty;
	}

	/// <summary>
	/// Creates a <see cref="ShortOverview"/> from text, cut after the word that the specified length ends in.
	/// </summary>
	/// <remarks>
	/// The text keeps its first <paramref name="howLong"/> characters and the rest of the word that they end in, with
	/// white space trimmed from both ends, and then ends with three periods.
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
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(howLong);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(howLong, text.Length);

		string shorted = Truncate(text, howLong);
		return new ShortOverview(shorted);
	}

	/// <summary>
	/// Cuts text after the word that the specified length ends in and ends it with three periods.
	/// </summary>
	/// <remarks>
	/// <para>The word runs to the first white space at or after the last kept character, and the periods take that white
	/// space's place. When no white space follows, the text is kept to its end.</para>
	/// <para>The kept text is trimmed of white space at both ends, so the periods never follow white space.</para>
	/// <para>The kept text is copied once, straight into the new string, with no buffer in between.</para>
	/// </remarks>
	/// <param name="text">The text to shorten.</param>
	/// <param name="shortenedLength">
	/// The number of characters to keep before looking for the end of a word, from 1 through the length of
	/// <paramref name="text"/>.
	/// </param>
	/// <returns>The kept text, with white space trimmed from both ends, followed by three periods.</returns>
	private static string Truncate(ReadOnlySpan<char> text, int shortenedLength)
	{
		int end = shortenedLength - 1;
		while ((uint)end < (uint)text.Length && !char.IsWhiteSpace(text[end]))
		{
			end++;
		}

		ReadOnlySpan<char> kept = text.UnsafeSlice(0, end).Trim();
		return string.Concat(kept, _ellipsis);
	}
}
