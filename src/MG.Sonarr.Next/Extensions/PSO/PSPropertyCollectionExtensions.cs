using System.Management.Automation;

namespace MG.Sonarr.Next.Extensions.PSO;

/// <summary>
/// Provides helpers for removing members from PowerShell member collections.
/// </summary>
public static class PSMemberInfoCollectionExtensions
{
	/// <summary>
	/// Removes all members that satisfy the specified predicate.
	/// </summary>
	/// <typeparam name="T">The member type.</typeparam>
	/// <param name="collection">The member collection to modify.</param>
	/// <param name="predicate">The predicate used to select members to remove.</param>
	public static void RemoveAll<T>(this PSMemberInfoCollection<T> collection, Func<T, bool> predicate)
		where T : PSMemberInfo
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
	/// <typeparam name="T">The member type.</typeparam>
	/// <param name="collection">The member collection to modify.</param>
	/// <param name="propertyNames">The member names to remove.</param>
	public static void RemoveMany<T>(this PSMemberInfoCollection<T> collection, params ReadOnlySpan<string> propertyNames) where T : PSMemberInfo
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
