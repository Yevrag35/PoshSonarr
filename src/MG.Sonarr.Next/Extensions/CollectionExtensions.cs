using MG.Sonarr.Next.Collections;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension members for collection types.
/// </summary>
internal static class CollectionExtensions
{
	/// <summary>
	/// Provides extension members for <see cref="ICollection{T}"/> instances.
	/// </summary>
	/// <typeparam name="T">The element type in the collection.</typeparam>
	/// <param name="collection">The collection to add to.</param>
	extension<T>(ICollection<T> collection)
	{
		/// <summary>
		/// Adds the elements of a span to <paramref name="collection"/>.
		/// </summary>
		/// <remarks>
		/// The elements are added one at a time through <see cref="ICollection{T}.Add(T)"/>, in order, so the
		/// collection's implementation decides where each one goes and whether it keeps a duplicate.
		/// </remarks>
		/// <param name="values">The elements to add.</param>
		internal void AddMany(params ReadOnlySpan<T> values)
		{
			foreach (T value in values)
			{
				collection.Add(value);
			}
		}
	}
	/// <summary>
	/// Provides extension members for <see cref="IReadOnlyCollection{T}"/> instances.
	/// </summary>
	/// <typeparam name="T">The element type in the collection.</typeparam>
	/// <param name="collection">The collection to inspect.</param>
	extension<T>([NotNullWhen(false)] IReadOnlyCollection<T>? collection)
	{
		/// <summary>
		/// Gets a value that indicates whether <paramref name="collection"/> is null or contains no elements.
		/// </summary>
		/// <value><see langword="true"/> if <paramref name="collection"/> is null or empty; otherwise, <see langword="false"/>.</value>
		internal bool IsNullOrEmpty => collection is null || collection.Count == 0;
	}
	/// <summary>
	/// Provides extension members for <see cref="List{T}"/> instances.
	/// </summary>
	/// <typeparam name="T">The element type in the list.</typeparam>
	/// <param name="list">The list to inspect.</param>
	extension<T>([NotNullWhen(false)] List<T>? list)
	{
		/// <summary>
		/// Gets a value that indicates whether <paramref name="list"/> is null or contains no elements.
		/// </summary>
		/// <value><see langword="true"/> if <paramref name="list"/> is null or empty; otherwise, <see langword="false"/>.</value>
		internal bool IsNullOrEmpty => list is null || list.Count == 0;
	}
	/// <summary>
	/// Provides extension members for <see cref="SortedSet{T}"/> instances.
	/// </summary>
	/// <typeparam name="T">The element type in the set.</typeparam>
	/// <param name="set">The set to inspect.</param>
	extension<T>([NotNullWhen(false)] SortedSet<T>? set)
	{
		/// <summary>
		/// Gets a value that indicates whether <paramref name="set"/> is null or contains no elements.
		/// </summary>
		/// <value><see langword="true"/> if <paramref name="set"/> is null or empty; otherwise, <see langword="false"/>.</value>
		internal bool IsNullOrEmpty => set is null || set.Count == 0;
	}
	/// <summary>
	/// Provides extension members for <see cref="WildcardSet"/> instances.
	/// </summary>
	/// <param name="set">The set to inspect.</param>
	extension([NotNullWhen(false)] WildcardSet? set)
	{
		/// <summary>
		/// Gets a value that indicates whether <paramref name="set"/> is null or contains no elements.
		/// </summary>
		/// <value><see langword="true"/> if <paramref name="set"/> is null or empty; otherwise, <see langword="false"/>.</value>
		internal bool IsNullOrEmpty => set is null || set.Count == 0;
	}
}
