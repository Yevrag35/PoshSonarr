using MG.Sonarr.Next.Models;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Json.Converters;

public sealed class ImmutableArrayConverter<T> : JsonConverter<ImmutableArray<T>> where T : SonarrObject, ISerializableNames<T>, new()
{
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
