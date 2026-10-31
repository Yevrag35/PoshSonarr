namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IList{T}"/> implementations.
/// </summary>
public static class ListExtensions
{
	/// <summary>
	/// Adds the elements of a span to the end of a list.
	/// </summary>
	/// <remarks>
	/// The elements are added one at a time through <see cref="ICollection{T}.Add(T)"/>, in order.
	/// </remarks>
	/// <typeparam name="T">The type of the list's elements.</typeparam>
	/// <param name="list">The list to add the elements to.</param>
	/// <param name="values">The elements to add.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="list"/> is null.</exception>
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
