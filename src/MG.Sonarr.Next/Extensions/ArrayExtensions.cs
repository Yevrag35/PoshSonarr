namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods for arrays.
/// </summary>
public static class ArrayExtensions
{
	/// <summary>
	/// Determines whether an array is null or has no elements.
	/// </summary>
	/// <param name="array">The array to check.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="array"/> is <see langword="null"/> or its length is 0; otherwise,
	/// <see langword="false"/>, which also tells the compiler that <paramref name="array"/> isn't
	/// <see langword="null"/>.
	/// </returns>
	public static bool IsNullOrEmpty([NotNullWhen(false)] this Array? array)
	{
		return array is null || array.Length == 0;
	}
}
