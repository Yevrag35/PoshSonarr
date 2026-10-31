using MG.Sonarr.Next.Models;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Json.Converters;

/// <summary>
/// Provides a converter that reads and writes a JSON array of models as an <see cref="ImmutableArray{T}"/>.
/// </summary>
/// <remarks>
/// A connection registers one for each model that a property holds as an immutable array, such as a provider's
/// <c>Fields</c> and a field's <c>SelectOptions</c>. Each item goes through the model's own converter, so the items
/// are deserialized but not tagged.
/// </remarks>
/// <typeparam name="T">The type of the array's models.</typeparam>
public sealed class ImmutableArrayConverter<T> : JsonConverter<ImmutableArray<T>> where T : SonarrObject, ISerializableNames<T>, new()
{
	/// <summary>
	/// Reads a JSON array of models.
	/// </summary>
	/// <remarks>
	/// The array that the serializer reads becomes the immutable array's storage without a copy.
	/// </remarks>
	/// <param name="reader">The reader positioned at the start of the array.</param>
	/// <param name="typeToConvert">The type to convert the value to.</param>
	/// <param name="options">The serializer options that each item is read with.</param>
	/// <returns>
	/// The array's models, or <see cref="ImmutableArray{T}.Empty"/> when the array is empty.
	/// </returns>
	/// <exception cref="JsonException">
	/// Thrown when the reader isn't positioned at the start of a JSON array, or when an item can't be read.
	/// </exception>
	public override ImmutableArray<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartArray)
		{
			throw new JsonException($"Converter expected a JSON array but got {reader.TokenType}.");
		}

		T[]? array = JsonSerializer.Deserialize<T[]>(ref reader, options);

		return array is not null && array.Length > 0
			? ImmutableCollectionsMarshal.AsImmutableArray(array)
			: ImmutableArray<T>.Empty;
	}

	/// <summary>
	/// Writes an immutable array of models as a JSON array.
	/// </summary>
	/// <remarks>
	/// Each model is written by its runtime type. A default array is written as an empty JSON array.
	/// </remarks>
	/// <param name="writer">The writer to write the array to.</param>
	/// <param name="value">The array to write.</param>
	/// <param name="options">The serializer options that each item is written with.</param>
	public override void Write(Utf8JsonWriter writer, ImmutableArray<T> value, JsonSerializerOptions options)
	{
		writer.WriteStartArray();
		if (value.IsDefaultOrEmpty)
		{
			writer.WriteEndArray();
			return;
		}

		foreach (T item in value)
		{
			JsonSerializer.Serialize(writer, item, item.GetType(), options);
		}

		writer.WriteEndArray();
	}
}
