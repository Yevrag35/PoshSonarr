namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods for read-only collections.
/// </summary>
public static class CollectionExtensions
{
	/// <summary>
	/// Determines whether a collection is null or has no elements.
	/// </summary>
	/// <typeparam name="T">The type of the collection's elements.</typeparam>
	/// <param name="collection">The collection to check.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="collection"/> is <see langword="null"/> or its count is 0; otherwise,
	/// <see langword="false"/>, which also tells the compiler that <paramref name="collection"/> isn't
	/// <see langword="null"/>.
	/// </returns>
	public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IReadOnlyCollection<T>? collection)
	{
		return collection is null || collection.Count == 0;
	}
}
