namespace MG.Sonarr.Next.Collections;

/// <summary>
/// Represents a read-only dictionary containing zero or one key/value pair.
/// </summary>
[DebuggerStepThrough, DebuggerDisplay("Count = {Count}")]
internal sealed class OneStringDictionary : IReadOnlyDictionary<string, string>
{
	internal static readonly OneStringDictionary Empty = new();

	private readonly string _key;
	private readonly string _value;

	/// <summary>
	/// Gets a value that indicates whether this dictionary is empty.
	/// </summary>
	/// <value><see langword="true"/> when this dictionary has no key/value pair; otherwise, <see langword="false"/>.</value>
	public bool IsEmpty => _key is "";

	/// <summary>
	/// Gets the single key in this dictionary.
	/// </summary>
	/// <value>The stored key, or an empty string when this dictionary is empty.</value>
	public string Key => _key;

	/// <summary>
	/// Gets the single value in this dictionary.
	/// </summary>
	/// <value>The stored value, or an empty string when this dictionary is empty.</value>
	public string Value => _value ?? string.Empty;

	public int Count => this.IsEmpty ? 0 : 1;
	IEnumerable<string> IReadOnlyDictionary<string, string>.Keys
	{
		[DebuggerStepThrough]
		get => [this.Key];
	}
	IEnumerable<string> IReadOnlyDictionary<string, string>.Values
	{
		[DebuggerStepThrough]
		get => [this.Value];
	}
	public string this[string key] => this.GetValueIfKeyIsSame(key, failOnNotEqual: true);

	/// <summary>
	/// Initializes a new empty <see cref="OneStringDictionary"/> instance.
	/// </summary>
	[DebuggerStepThrough]
	private OneStringDictionary()
	{
		_key = string.Empty;
		_value = string.Empty;
	}
	/// <summary>
	/// Initializes a new <see cref="OneStringDictionary"/> instance with the specified key and value.
	/// </summary>
	/// <param name="key">The dictionary key.</param>
	/// <param name="value">The dictionary value.</param>
	/// <exception cref="System.ArgumentException">Thrown when <paramref name="key"/> or <paramref name="value"/> is null or empty.</exception>
	internal OneStringDictionary(string key, string value)
	{
		ArgumentException.ThrowIfNullOrEmpty(key);
		ArgumentException.ThrowIfNullOrEmpty(value);

		_key = key;
		_value = value;
	}

	/// <summary>
	/// Initializes a new <see cref="OneStringDictionary"/> instance from the specified key/value pair.
	/// </summary>
	/// <param name="pair">The key/value pair.</param>
	internal OneStringDictionary(KeyValuePair<string, string> pair)
		: this(pair.Key, pair.Value)
	{
	}

	/// <summary>
	/// Determines whether this dictionary contains the specified key.
	/// </summary>
	/// <param name="key">The key to locate.</param>
	/// <returns><see langword="true"/> when the key exists; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	public bool ContainsKey(string key)
	{
		ArgumentNullException.ThrowIfNull(key);
		return !this.IsEmpty
			&& key.AsSpan().Equals(_key, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Returns an enumerator that iterates through the dictionary.
	/// </summary>
	/// <returns>An enumerator for the dictionary contents.</returns>
	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		if (!this.IsEmpty)
		{
			KeyValuePair<string, string> kvp = new(_key, _value);
			return Enumerable.Repeat(kvp, 1).GetEnumerator();
		}

		return Enumerable.Empty<KeyValuePair<string, string>>().GetEnumerator();
	}
	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}

	/// <summary>
	/// Attempts to get the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key to locate.</param>
	/// <param name="value">The stored value when found; otherwise, an empty string.</param>
	/// <returns><see langword="true"/> when the key exists; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	public bool TryGetValue(string key, out string value)
	{
		ArgumentNullException.ThrowIfNull(key);

		value = this.GetValueIfKeyIsSame(key, failOnNotEqual: false);
		return !string.IsNullOrEmpty(value);
	}

	/// <summary>
	/// Creates a <see cref="OneStringDictionary"/> from the first pair in the specified collection.
	/// </summary>
	/// <param name="collection">The source collection.</param>
	/// <returns>A dictionary containing the first pair in <paramref name="collection"/>, or an empty dictionary when no pairs are present.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="collection"/> is null.</exception>
	internal static OneStringDictionary FromEnumerable(IEnumerable<KeyValuePair<string, string>> collection)
	{
		ArgumentNullException.ThrowIfNull(collection);
		if (collection.TryGetNonEnumeratedCount(out int count) && count == 0)
		{
			goto returnEmpty;
		}

		foreach (KeyValuePair<string, string> kvp in collection)
		{
			return new OneStringDictionary(kvp);
		}

	returnEmpty:
		return Empty;
	}
	/// <summary>
	/// Gets the stored value when the specified key matches the internal key.
	/// </summary>
	/// <param name="key">The key to evaluate.</param>
	/// <param name="failOnNotEqual">A value that indicates whether a key mismatch throws an exception.</param>
	/// <returns>The stored value when keys match; otherwise, an empty string when <paramref name="failOnNotEqual"/> is <see langword="false"/>.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
	/// <exception cref="System.Collections.Generic.KeyNotFoundException">Thrown when keys do not match and <paramref name="failOnNotEqual"/> is <see langword="true"/>.</exception>
	private string GetValueIfKeyIsSame(string key, bool failOnNotEqual)
	{
		ArgumentNullException.ThrowIfNull(key);
		if (!this.IsEmpty && key.Equals(_key, StringComparison.OrdinalIgnoreCase))
		{
			return _value;
		}
		else if (failOnNotEqual)
		{
			throw new KeyNotFoundException($"{key} does not match the single key value of {_key}.");
		}

		return string.Empty;
	}
}
