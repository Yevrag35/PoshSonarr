namespace MG.Sonarr.Next.Extensions.Pwsh;

/// <summary>
/// Provides helpers for removing members from PowerShell member collections.
/// </summary>
internal static class PSMemberInfoCollectionExtensions
{
	/// <summary>
	/// Provides extension methods that remove members from a <see cref="PSMemberInfoCollection{T}"/>.
	/// </summary>
	/// <typeparam name="T">The member type.</typeparam>
	/// <param name="collection">The member collection to modify.</param>
	extension<T>(PSMemberInfoCollection<T> collection) where T : PSMemberInfo
	{
		/// <summary>
		/// Removes multiple members by name.
		/// </summary>
		/// <param name="propertyNames">The member names to remove.</param>
		internal void RemoveMany(params ReadOnlySpan<string> propertyNames)
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
