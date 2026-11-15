namespace MG.Sonarr.Next.Strings;

/// <summary>
/// Defines the type of matching a <see cref="Wildcard"/> instance will use when comparing the pattern and input.
/// </summary>
public enum WildcardMatchType
{
	/// <summary>
	/// Indicates that the pattern is empty and that no input will ever match.
	/// </summary>
	None = 0,
	/// <summary>
	/// Indicates that the input must contain all or portions of the pattern.
	/// </summary>
	Like = 1,
	/// <summary>
	/// Indicates that the input must start with the pattern.
	/// </summary>
	StartsWith = 2,
	/// <summary>
	/// Indicates that the input must end with the pattern.
	/// </summary>
	EndsWith = 3,
	/// <summary>
	/// Indicates that the input must match the pattern exactly.
	/// </summary>
	Exact = 4,
	/// <summary>
	/// Indicates that the pattern is a singular <c>*</c> character and will match any input, including empty input.
	/// </summary>
	All = 10,
}