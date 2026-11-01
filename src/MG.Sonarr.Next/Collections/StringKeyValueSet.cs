namespace MG.Sonarr.Next.Collections;

/// <summary>
/// Represents a case-insensitive sorted set of string keys and associated values.
/// </summary>
/// <remarks>
/// Keys compare and sort with <see cref="StringComparer.OrdinalIgnoreCase"/>, as <see cref="StringSet"/>'s strings do,
/// so keys that differ only in case are one key, and keys that differ in any other way, such as in a soft hyphen or
/// in a precomposed letter against a letter and a combining mark, are two.
/// </remarks>
/// <typeparam name="T">The value type.</typeparam>
[DebuggerDisplay("Count = {Count}")]
public sealed class StringKeyValueSet<T> : SortedSet<KeyValuePair<string, T>>
{
	/// <summary>
	/// Gets or sets the value associated with the specified key.
	/// </summary>
	/// <remarks>
	/// Setting a <see langword="null"/> value changes nothing, since the set holds no <see langword="null"/> value.
	/// </remarks>
	/// <param name="key">The key to locate or assign.</param>
	/// <exception cref="ArgumentException">Thrown when a value is set for a key that is null or empty.</exception>
	/// <value>The matching value, or the default value when the key is not present.</value>
	public T? this[string key]
	{
		get => this.TryGetValue(key, out T? value)
			? value
			: default;
		set
		{
			ArgumentException.ThrowIfNullOrEmpty(key);
			if (value is null)
			{
				return;
			}

			var kvp = ToKeyPair(key, value);
			this.Remove(kvp);
			this.Add(kvp);
		}
	}

	/// <summary>
	/// Initializes a new <see cref="StringKeyValueSet{T}"/> instance.
	/// </summary>
	public StringKeyValueSet()
		: base(KeyValueComparer<T>.Instance)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="StringKeyValueSet{T}"/> instance with the specified values.
	/// </summary>
	/// <param name="values">The key/value pairs to add to the set.</param>
	public StringKeyValueSet(IEnumerable<KeyValuePair<string, T>> values)
		: base(values, KeyValueComparer<T>.Instance)
	{
	}

	/// <summary>
	/// Adds the specified key and value to the set.
	/// </summary>
	/// <param name="key">The key to add.</param>
	/// <param name="value">The value to add.</param>
	/// <returns><see langword="true"/> when the pair is added; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty.</exception>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public bool Add(string key, T value)
	{
		ArgumentException.ThrowIfNullOrEmpty(key);
		ArgumentNullException.ThrowIfNull(value);

		return this.Add(ToKeyPair(key, value));
	}

	/// <summary>
	/// Determines whether the set contains the specified key.
	/// </summary>
	/// <param name="key">The key to locate.</param>
	/// <returns><see langword="true"/> when the key exists; otherwise, <see langword="false"/>.</returns>
	public bool ContainsKey(string key)
	{
		return this.Contains(ToKeyPair(key));
	}

	/// <summary>
	/// Removes the pair with the specified key from the set.
	/// </summary>
	/// <param name="key">The key to remove.</param>
	/// <returns><see langword="true"/> when a matching key is removed; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty.</exception>
	public bool Remove(string key)
	{
		ArgumentException.ThrowIfNullOrEmpty(key);
		return this.Remove(ToKeyPair(key));
	}

	/// <summary>
	/// Tries to get the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key to locate.</param>
	/// <param name="value">The matching value when found; otherwise, the default value.</param>
	/// <returns><see langword="true"/> when a matching key is found; otherwise, <see langword="false"/>.</returns>
	public bool TryGetValue(string key, [NotNullWhen(true)] out T? value)
	{
		value = default;
		if (this.TryGetValue(ToKeyPair(key), out KeyValuePair<string, T> kvp))
		{
			value = kvp.Value;
			return value is not null;
		}

		return false;
	}

	/// <summary>
	/// Creates a key/value pair with a default value for key-only comparisons.
	/// </summary>
	/// <param name="key">The key to wrap.</param>
	/// <returns>A key/value pair with the specified key.</returns>
	private static KeyValuePair<string, T> ToKeyPair(string key)
	{
		return new(key, default!);
	}

	/// <summary>
	/// Creates a key/value pair from the specified key and value.
	/// </summary>
	/// <param name="key">The key to wrap.</param>
	/// <param name="value">The value to wrap.</param>
	/// <returns>A key/value pair with the specified key and value.</returns>
	private static KeyValuePair<string, T> ToKeyPair(string key, T value)
	{
		return new(key, value);
	}

}

/// <summary>
/// Represents the key comparer that every <see cref="StringKeyValueSet{T}"/> of one value type shares.
/// </summary>
/// <remarks>
/// The runtime creates <see cref="Instance"/> once for each <typeparamref name="T"/>, safely from any number of threads,
/// as the module's runspaces share it.
/// </remarks>
/// <typeparam name="T">The value type.</typeparam>
file sealed class KeyValueComparer<T> : IComparer<KeyValuePair<string, T>>
{
	/// <summary>
	/// The comparer for <typeparamref name="T"/>.
	/// </summary>
	internal static readonly KeyValueComparer<T> Instance = new();

	/// <summary>
	/// Initializes a new <see cref="KeyValueComparer{T}"/> instance.
	/// </summary>
	private KeyValueComparer()
	{
	}

	/// <summary>
	/// Compares two key/value pairs by their keys, with <see cref="StringComparer.OrdinalIgnoreCase"/>.
	/// </summary>
	/// <param name="x">The first pair to compare.</param>
	/// <param name="y">The second pair to compare.</param>
	/// <returns>
	/// A value less than zero when <paramref name="x"/> sorts before <paramref name="y"/>;
	/// zero when they are equal; otherwise, a value greater than zero.
	/// </returns>
	public int Compare(KeyValuePair<string, T> x, KeyValuePair<string, T> y)
	{
		return StringComparer.OrdinalIgnoreCase.Compare(x.Key, y.Key);
	}
}

