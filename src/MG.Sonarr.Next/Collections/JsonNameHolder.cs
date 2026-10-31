namespace MG.Sonarr.Next.Collections;

/// <summary>
/// Represents paired name maps used for serialization and deserialization.
/// </summary>
internal readonly struct JsonNameHolder
{
	private static readonly EmptyNameDictionary<string> _empty = [];

	readonly IReadOnlyDictionary<string, string> _deserialization;
	readonly IReadOnlyDictionary<string, string> _serialization;

	/// <summary>
	/// Gets the name map used during deserialization.
	/// </summary>
	/// <value>A read-only dictionary keyed by incoming names.</value>
	public IReadOnlyDictionary<string, string> DeserializationNames => _deserialization ?? _empty;

	/// <summary>
	/// Gets the name map used during serialization.
	/// </summary>
	/// <value>A read-only dictionary keyed by outgoing names.</value>
	public IReadOnlyDictionary<string, string> SerializationNames => _serialization ?? _empty;

	/// <summary>
	/// Initializes a new <see cref="JsonNameHolder"/> instance with the specified name maps.
	/// </summary>
	/// <param name="serialization">The serialization name map.</param>
	/// <param name="deserialization">The deserialization name map.</param>
	internal JsonNameHolder(IReadOnlyDictionary<string, string>? serialization, IReadOnlyDictionary<string, string>? deserialization)
	{
		_deserialization = deserialization ?? _empty;
		_serialization = serialization ?? _empty;
	}

	/// <summary>
	/// Creates a <see cref="JsonNameHolder"/> from a single deserialization/serialization name pair.
	/// </summary>
	/// <param name="key">The deserialization name.</param>
	/// <param name="value">The serialization name.</param>
	/// <returns>A holder that maps both directions for the supplied pair.</returns>
	/// <exception cref="System.ArgumentException">Thrown when <paramref name="key"/> or <paramref name="value"/> is null or empty.</exception>
	internal static JsonNameHolder FromSingleNamePair(string key, string value)
	{
		ArgumentException.ThrowIfNullOrEmpty(key);
		ArgumentException.ThrowIfNullOrEmpty(value);

		OneStringDictionary des = new(key, value);
		OneStringDictionary ser = new(value, key);

		return new(ser, des);
	}

	/// <summary>
	/// Creates a <see cref="JsonNameHolder"/> from deserialization name pairs.
	/// </summary>
	/// <param name="values">The deserialization name pairs.</param>
	/// <returns>A holder containing deserialization and reversed serialization name maps.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="values"/> is null.</exception>
	internal static JsonNameHolder FromDeserializationNamePairs(IEnumerable<KeyValuePair<string, string>> values)
	{
		ArgumentNullException.ThrowIfNull(values);

		if (values is IReadOnlyDictionary<string, string> dict)
		{
			return FromSingleDictionary(dict);
		}
		else if (values.TryGetNonEnumeratedCount(out int count) && count == 1)
		{
			var deserOne = OneStringDictionary.FromEnumerable(values);
			return new(new OneStringDictionary(deserOne.Value, deserOne.Key), deserOne);
		}
		else
		{
			Dictionary<string, string> des = new(count, StringComparer.InvariantCultureIgnoreCase);
			Dictionary<string, string> ser = new(count, StringComparer.InvariantCultureIgnoreCase);
			foreach (KeyValuePair<string, string> kvp in values)
			{
				des.Add(kvp.Key, kvp.Value);
				ser.Add(kvp.Value, kvp.Key);
			}

			return new(ser.ToReadOnly(), des.ToReadOnly());
		}
	}

	/// <summary>
	/// Creates a <see cref="JsonNameHolder"/> from a read-only dictionary.
	/// </summary>
	/// <param name="dict">The dictionary containing deserialization mappings.</param>
	/// <returns>A holder configured for the dictionary size and contents.</returns>
	private static JsonNameHolder FromSingleDictionary(IReadOnlyDictionary<string, string> dict)
	{
		return dict.Count switch
		{
			0 => new(_empty, _empty),
			1 => new(OneStringDictionary.FromEnumerable(dict), dict),
			>= 2 => new(ReverseDictionary(dict), dict),
			_ => throw new UnreachableException("How could you have a negative count???"),
		};
	}

	/// <summary>
	/// Reverses a dictionary by swapping keys and values.
	/// </summary>
	/// <param name="forwardDict">The dictionary to reverse.</param>
	/// <returns>A read-only dictionary whose keys and values are swapped from <paramref name="forwardDict"/>.</returns>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="forwardDict"/> is null.</exception>
	private static IReadOnlyDictionary<string, string> ReverseDictionary(IReadOnlyDictionary<string, string> forwardDict)
	{
		ArgumentNullException.ThrowIfNull(forwardDict);

		IEqualityComparer<string> comparer = forwardDict is Dictionary<string, string> realDict
			? realDict.Comparer
			: StringComparer.InvariantCultureIgnoreCase;

		Dictionary<string, string> reversed = new(forwardDict.Count, comparer);
		foreach (var kvp in forwardDict)
		{
			reversed.Add(kvp.Value, kvp.Key);
		}

		return reversed.ToReadOnly();
	}
}
