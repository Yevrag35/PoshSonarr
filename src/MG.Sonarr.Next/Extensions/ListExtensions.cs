namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Custom extension methods for <see cref="List{T}"/> and <see cref="IList{T}"/> implementations.
/// </summary>
public static class ListExtensions
{
	/// <summary>
	/// Adds all elements from the specified span into the current list.
	/// </summary>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	/// <param name="list">The list to copy the elements into.</param>
	/// <param name="values">
	///     The elements that will be added into <paramref name="list"/>.
	/// </param>
	/// <exception cref="ArgumentNullException"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AddMany<T>(this IList<T> list, params ReadOnlySpan<T> values)
	{
		ArgumentNullException.ThrowIfNull(list);
		foreach (T value in values)
		{
			list.Add(value);
		}
	}
}
