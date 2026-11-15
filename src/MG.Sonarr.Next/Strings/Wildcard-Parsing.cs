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
}