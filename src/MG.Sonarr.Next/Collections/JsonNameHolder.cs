namespace MG.Sonarr.Next.Collections;

/// <summary>
/// Represents paired name maps used for serialization and deserialization.
/// </summary>
/// <remarks>
/// <para>
/// The serialization map is the reverse of the deserialization map, whatever shape the pairs come in.
/// </para>
/// <para>
/// A map built from pairs compares names with <see cref="StringComparer.OrdinalIgnoreCase"/>, as
/// <see cref="OneStringDictionary"/> does, since the names are JSON property names and not linguistic text. A
/// dictionary passed to <see cref="FromDeserializationNamePairs(IEnumerable{KeyValuePair{string, string}})"/> is the
/// deserialization map as it is, and its reverse takes the dictionary's comparer when it's a
/// <see cref="Dictionary{TKey, TValue}"/>.
/// </para>
/// </remarks>
internal readonly struct JsonNameHolder
{
	private static readonly EmptyNameDictionary<string> s_empty = [];

	private readonly IReadOnlyDictionary<string, string> _deserialization;
	private readonly IReadOnlyDictionary<string, string> _serialization;

	/// <summary>
	/// Gets the name map used during deserialization.
	/// </summary>
	/// <value>A read-only dictionary keyed by incoming names.</value>
	public IReadOnlyDictionary<string, string> DeserializationNames => _deserialization ?? s_empty;

	/// <summary>
	/// Gets the name map used during serialization.
	/// </summary>
	/// <value>A read-only dictionary keyed by outgoing names.</value>
	public IReadOnlyDictionary<string, string> SerializationNames => _serialization ?? s_empty;

	/// <summary>
	/// Initializes a new <see cref="JsonNameHolder"/> instance with the specified name maps.
	/// </summary>
	/// <param name="serialization">The serialization name map.</param>
	/// <param name="deserialization">The deserialization name map.</param>
	internal JsonNameHolder(IReadOnlyDictionary<string, string>? serialization, IReadOnlyDictionary<string, string>? deserialization)
	{
		_deserialization = deserialization ?? s_empty;
		_serialization = serialization ?? s_empty;
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
	/// <exception cref="System.ArgumentNullException">
	/// Thrown when <paramref name="values"/> is null, or when one of its names is null.
	/// </exception>
	/// <exception cref="System.ArgumentException">
	/// Thrown when two pairs have the same deserialization name or the same serialization name, or when a name is empty.
	/// </exception>
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
			Dictionary<string, string> des = new(count, StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> ser = new(count, StringComparer.OrdinalIgnoreCase);
			foreach (KeyValuePair<string, string> kvp in values)
			{
				ArgumentException.ThrowIfNullOrEmpty(kvp.Key, nameof(values));
				ArgumentException.ThrowIfNullOrEmpty(kvp.Value, nameof(values));

				des.Add(kvp.Key, kvp.Value);
				ser.Add(kvp.Value, kvp.Key);
			}

			return new(ser.ToReadOnly(), des.ToReadOnly());
		}
	}

	/// <summary>
	/// Creates a <see cref="JsonNameHolder"/> from a read-only dictionary.
	/// </summary>
	/// <remarks>
	/// <see cref="ReverseDictionary"/> reverses a <see cref="Dictionary{TKey, TValue}"/> even when it holds one entry, so
	/// that the reverse takes its comparer. A <see cref="OneStringDictionary"/> always ignores case.
	/// </remarks>
	/// <param name="dict">The dictionary containing deserialization mappings.</param>
	/// <returns>A holder configured for the dictionary size and contents.</returns>
	private static JsonNameHolder FromSingleDictionary(IReadOnlyDictionary<string, string> dict)
	{
		return dict.Count switch
		{
			0 => new(s_empty, s_empty),
			1 when dict is not Dictionary<string, string> => new(ReverseSingle(dict), dict),
			>= 1 => new(ReverseDictionary(dict), dict),
			_ => throw new UnreachableException("How could you have a negative count???"),
		};
	}

	/// <summary>
	/// Reverses a dictionary that holds one entry by swapping its key and value.
	/// </summary>
	/// <param name="forwardDict">The dictionary to reverse.</param>
	/// <returns>A dictionary whose one key is the value of <paramref name="forwardDict"/>'s entry, and whose value is its key.</returns>
	private static OneStringDictionary ReverseSingle(IReadOnlyDictionary<string, string> forwardDict)
	{
		OneStringDictionary forward = OneStringDictionary.FromEnumerable(forwardDict);
		return new(forward.Value, forward.Key);
	}

	/// <summary>
	/// Reverses a dictionary by swapping keys and values.
	/// </summary>
	/// <remarks>
	/// The reverse of a <see cref="Dictionary{TKey, TValue}"/> takes its comparer, and the reverse of any other
	/// dictionary compares names with <see cref="StringComparer.OrdinalIgnoreCase"/>.
	/// </remarks>
	/// <param name="forwardDict">The dictionary to reverse.</param>
	/// <returns>A dictionary whose keys and values are swapped from <paramref name="forwardDict"/>.</returns>
	/// <exception cref="System.ArgumentNullException">
	/// Thrown when <paramref name="forwardDict"/> is null, or when one of its keys or values is null.
	/// </exception>
	/// <exception cref="System.ArgumentException">
	/// Thrown when two entries in <paramref name="forwardDict"/> have the same value, or when one of its keys or values
	/// is empty.
	/// </exception>
	private static Dictionary<string, string> ReverseDictionary(IReadOnlyDictionary<string, string> forwardDict)
	{
		ArgumentNullException.ThrowIfNull(forwardDict);

		IEqualityComparer<string> comparer = forwardDict is Dictionary<string, string> realDict
			? realDict.Comparer
			: StringComparer.OrdinalIgnoreCase;

		Dictionary<string, string> reversed = new(forwardDict.Count, comparer);
		foreach (var kvp in forwardDict)
		{
			ArgumentException.ThrowIfNullOrEmpty(kvp.Key, nameof(forwardDict));
			ArgumentException.ThrowIfNullOrEmpty(kvp.Value, nameof(forwardDict));

			reversed.Add(kvp.Value, kvp.Key);
		}

		return reversed;
	}
}
