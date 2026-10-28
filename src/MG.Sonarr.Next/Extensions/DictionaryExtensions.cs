using System.Collections.ObjectModel;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Custom extension methods for specific types of <see cref="Dictionary{TKey, TValue}"/>.
/// </summary>
public static class DictionaryExtensions
{
	[SuppressMessage("Style", "IDE0008:Use explicit type", Justification = "<Pending>")]
	public static bool ContainsKey<T>(this Dictionary<string, T> dictionary, ReadOnlySpan<char> key)
	{
		var lookup =
#if NET9_0_OR_GREATER
			dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
#else
                dictionary;
#endif

		var lookupKey =
#if NET9_0_OR_GREATER
			key;
#else
                key.ToString();
#endif
		return lookup.ContainsKey(lookupKey);
	}

	[return: NotNullIfNotNull(nameof(dictionary))]
	public static ReadOnlyDictionary<TKey, TValue>? ToReadOnly<TKey, TValue>(this Dictionary<TKey, TValue>? dictionary) where TKey : notnull
	{
		if (dictionary is null)
		{
			return null;
		}

		return new ReadOnlyDictionary<TKey, TValue>(dictionary);
	}
}
