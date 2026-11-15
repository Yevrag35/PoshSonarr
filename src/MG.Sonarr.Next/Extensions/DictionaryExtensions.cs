using System.Collections.ObjectModel;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Dictionary{TKey, TValue}"/>.
/// </summary>
internal static class DictionaryExtensions
{
	/// <summary>
	/// Provides extension methods that look up a key in a <see cref="Dictionary{TKey, TValue}"/> given as a span of characters.
	/// </summary>
	/// <typeparam name="T">The type of the dictionary's values.</typeparam>
	/// <param name="dictionary">The dictionary to extend.</param>
	extension<T>(Dictionary<string, T> dictionary)
	{
		/// <summary>
		/// Determines whether the dictionary contains the specified key, given as a span of characters.
		/// </summary>
		/// <remarks>
		/// A dictionary whose comparer can compare a span with a <see cref="string"/>, as the default and ordinal comparers
		/// can, looks the span up without allocating. Any other comparer gets the key as a new <see cref="string"/>.
		/// </remarks>
		/// <param name="key">The key to look for.</param>
		/// <returns><see langword="true"/> if <paramref name="dictionary"/> contains <paramref name="key"/>; otherwise,
		/// <see langword="false"/>.</returns>
		internal bool ContainsKey(ReadOnlySpan<char> key)
		{
			return dictionary.TryGetAlternateLookup(out Dictionary<string, T>.AlternateLookup<ReadOnlySpan<char>> lookup)
				? lookup.ContainsKey(key)
				: dictionary.ContainsKey(key.ToString());
		}
	}

	/// <summary>
	/// Provides extension methods that wrap a <see cref="Dictionary{TKey, TValue}"/> in a read-only view.
	/// </summary>
	/// <typeparam name="TKey">The type of the dictionary's keys.</typeparam>
	/// <typeparam name="TValue">The type of the dictionary's values.</typeparam>
	/// <param name="dictionary">The dictionary to extend.</param>
	extension<TKey, TValue>(Dictionary<TKey, TValue>? dictionary) where TKey : notnull
	{
		/// <summary>
		/// Wraps a dictionary in a read-only view.
		/// </summary>
		/// <remarks>
		/// The view doesn't copy the dictionary, so a later change to <paramref name="dictionary"/> shows through it.
		/// </remarks>
		/// <returns>
		/// A new <see cref="ReadOnlyDictionary{TKey, TValue}"/> over <paramref name="dictionary"/>, or
		/// <see langword="null"/> when <paramref name="dictionary"/> is <see langword="null"/>.
		/// </returns>
		[return: NotNullIfNotNull(nameof(dictionary))]
		internal ReadOnlyDictionary<TKey, TValue>? ToReadOnly()
		{
			return dictionary is not null ? new ReadOnlyDictionary<TKey, TValue>(dictionary) : null;
		}
	}
}
