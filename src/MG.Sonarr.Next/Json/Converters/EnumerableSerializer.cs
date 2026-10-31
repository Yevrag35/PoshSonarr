using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Json.Converters;

/// <summary>
/// Provides a write-only converter that writes a sequence as a JSON array, one serialized item at a time.
/// </summary>
/// <remarks>
/// A property opts in with <see cref="JsonConverterAttribute"/>, as the episode file IDs of a rename command's body
/// do, so a lazily evaluated sequence goes out as an array.
/// </remarks>
/// <typeparam name="T">The type of the sequence's items.</typeparam>
public sealed class EnumerableSerializer<T> : JsonConverter<IEnumerable<T>> where T : notnull
{
	/// <summary>
	/// Throws a <see cref="NotSupportedException"/>, because the converter only writes.
	/// </summary>
	/// <param name="reader">The reader positioned at the value.</param>
	/// <param name="typeToConvert">The type to convert the value to.</param>
	/// <param name="options">The serializer options in use.</param>
	/// <returns>Nothing, since the method always throws.</returns>
	/// <exception cref="NotSupportedException">Thrown when the method is called.</exception>
	public override IEnumerable<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Writes a sequence as a JSON array, or a JSON null for a <see langword="null"/> sequence.
	/// </summary>
	/// <remarks>
	/// Each item is serialized as <typeparamref name="T"/> to a string of its own, which the writer then writes as a
	/// raw value, so each item allocates a string.
	/// </remarks>
	/// <param name="writer">The writer to write the array to.</param>
	/// <param name="value">The sequence to write.</param>
	/// <param name="options">The serializer options that each item is serialized with.</param>
	public override void Write(Utf8JsonWriter writer, IEnumerable<T> value, JsonSerializerOptions options)
	{
		if (!writer.TryWriteNullValue(value))
		{
			Type type = typeof(T);
			writer.WriteStartArray();
			foreach (T item in value)
			{
				writer.WriteRawValue(JsonSerializer.Serialize(item, type, options));
			}

			writer.WriteEndArray();
		}
	}
}
