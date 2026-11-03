namespace MG.Sonarr.Next.Extensions.PSO;

/// <summary>
/// Provides helpers for removing members from PowerShell member collections.
/// </summary>
public static class PSMemberInfoCollectionExtensions
{
	/// <summary>
	/// Provides extension methods that remove members from a <see cref="PSMemberInfoCollection{T}"/>.
	/// </summary>
	/// <typeparam name="T">The member type.</typeparam>
	/// <param name="collection">The member collection to modify.</param>
	extension<T>(PSMemberInfoCollection<T> collection) where T : PSMemberInfo
	{
		/// <summary>
		/// Removes all members that satisfy the specified predicate.
		/// </summary>
		/// <param name="predicate">The predicate used to select members to remove.</param>
		public void RemoveAll(Func<T, bool> predicate)
		{
			foreach (T item in collection)
			{
				if (predicate(item))
				{
					collection.Remove(item.Name);
				}
			}
		}

		/// <summary>
		/// Removes multiple members by name.
		/// </summary>
		/// <param name="propertyNames">The member names to remove.</param>
		public void RemoveMany(params ReadOnlySpan<string> propertyNames)
		{
			if (propertyNames.IsEmpty)
			{
				return;
			}

			foreach (string name in propertyNames)
			{
				collection.Remove(name);
			}
		}
	}
}
