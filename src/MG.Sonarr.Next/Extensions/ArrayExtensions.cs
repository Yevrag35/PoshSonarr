using MG.Sonarr.Next.Buffers;

namespace MG.Sonarr.Next.Extensions;

public static class ArrayExtensions
{
	/// <summary>
	/// Determines whether the specified array is null or has a length of zero.
	/// </summary>
	/// <param name="array">The array to test for nullity or emptiness.</param>
	/// <returns>true if the array is null or has a length of zero; otherwise, false.</returns>
	public static bool IsNullOrEmpty([NotNullWhen(false)] this Array? array)
	{
		return array is null || array.Length == 0;
	}
}
