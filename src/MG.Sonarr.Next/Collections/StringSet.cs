namespace MG.Sonarr.Next.Collections;

/// <summary>
/// Represents a case-insensitive sorted set of strings.
/// </summary>
public sealed class StringSet : SortedSet<string>
{
	/// <summary>
	/// Initializes a new <see cref="StringSet"/> instance.
	/// </summary>
	public StringSet()
		: base(StringComparer.OrdinalIgnoreCase)
	{
	}
}

