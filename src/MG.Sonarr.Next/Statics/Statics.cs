namespace MG.Sonarr.Next;

/// <summary>
/// Provides shared culture and formatting defaults.
/// </summary>
public static class Statics
{
	/// <summary>
	/// Represents the default culture used by the library.
	/// </summary>
	public static readonly CultureInfo DefaultCulture = CultureInfo.CurrentCulture;

	/// <summary>
	/// Represents the default format provider used by the library.
	/// </summary>
	public static readonly IFormatProvider DefaultProvider = DefaultCulture;
}