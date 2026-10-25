using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Extensions;

public static class SortedSetExtensions
{
	public static void AddRange<T>(this SortedSet<int> ids, params ReadOnlySpan<T> values) where T : IPipeable<T>
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
