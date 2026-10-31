using System.Collections.ObjectModel;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Dictionary{TKey, TValue}"/>.
/// </summary>
public static class DictionaryExtensions
{
	/// <summary>
	/// Determines whether the dictionary contains the specified key, given as a span of characters.
	/// </summary>
	/// <remarks>
	/// A dictionary whose comparer can compare a span with a <see cref="string"/>, as the default and ordinal comparers
	/// can, looks the span up without allocating. Any other comparer gets the key as a new <see cref="string"/>.
	/// </remarks>
	/// <typeparam name="T">The type of the dictionary's values.</typeparam>
	/// <param name="dictionary">The dictionary to search.</param>
	/// <param name="key">The key to look for.</param>
	/// <returns><see langword="true"/> if <paramref name="dictionary"/> contains <paramref name="key"/>; otherwise,
	/// <see langword="false"/>.</returns>
	public static bool ContainsKey<T>(this Dictionary<string, T> dictionary, ReadOnlySpan<char> key)
	{
#if NET9_0_OR_GREATER
		return dictionary.TryGetAlternateLookup(out Dictionary<string, T>.AlternateLookup<ReadOnlySpan<char>> lookup)
			? lookup.ContainsKey(key)
			: dictionary.ContainsKey(key.ToString());
#else
		return dictionary.ContainsKey(key.ToString());
#endif
	}

	/// <summary>
	/// Wraps a dictionary in a read-only view.
	/// </summary>
	/// <remarks>
	/// The view doesn't copy the dictionary, so a later change to <paramref name="dictionary"/> shows through it.
	/// </remarks>
	/// <typeparam name="TKey">The type of the dictionary's keys.</typeparam>
	/// <typeparam name="TValue">The type of the dictionary's values.</typeparam>
	/// <param name="dictionary">The dictionary to wrap, or <see langword="null"/>.</param>
	/// <returns>
	/// A new <see cref="ReadOnlyDictionary{TKey, TValue}"/> over <paramref name="dictionary"/>, or
	/// <see langword="null"/> when <paramref name="dictionary"/> is <see langword="null"/>.
	/// </returns>
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
