namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IList{T}"/> implementations.
/// </summary>
public static class ListExtensions
{
	/// <summary>
	/// The extension block for <see cref="IList{T}"/> implementations.
	/// </summary>
	/// <typeparam name="T">The type of the list's elements.</typeparam>
	/// <param name="list">The list to extend.</param>
	extension<T>(IList<T> list)
	{
		/// <summary>
		/// Adds the elements of a span to the end of a list.
		/// </summary>
		/// <remarks>
		/// The elements are added one at a time through <see cref="ICollection{T}.Add(T)"/>, in order.
		/// </remarks>
		/// <param name="values">The elements to add.</param>
		public void AddMany(params ReadOnlySpan<T> values)
		{
			foreach (T value in values)
			{
				list.Add(value);
			}
		}
	}
}
