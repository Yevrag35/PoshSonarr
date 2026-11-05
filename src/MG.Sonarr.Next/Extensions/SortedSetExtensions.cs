using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods for sets of IDs.
/// </summary>
public static class SortedSetExtensions
{
	/// <summary>
	/// Provides extension methods that add to a <see cref="SortedSet{T}"/> of IDs.
	/// </summary>
	/// <param name="ids">The set to add the IDs to.</param>
	extension(SortedSet<int> ids)
	{
		/// <summary>
		/// Adds the IDs of piped objects to a set of IDs.
		/// </summary>
		/// <remarks>
		/// An object whose <see cref="IPipeable{T}.GetId"/> returns <see langword="null"/> adds nothing, and an ID
		/// that's already in <paramref name="ids"/> is skipped.
		/// </remarks>
		/// <typeparam name="T">The pipeable interface that the objects give their IDs for.</typeparam>
		/// <param name="values">The objects whose IDs are added.</param>
		public void AddRange<T>(params ReadOnlySpan<T> values) where T : IPipeable<T>
		{
			foreach (T piped in values)
			{
				if (piped.GetId() is int id)
				{
					_ = ids.Add(id);
				}
			}
		}
	}
}
