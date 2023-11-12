using MG.Collections;
using MG.Sonarr.Next.Models;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Json.Converters
{
    public sealed class ReadOnlyListConverter<T> : JsonConverter<ReadOnlyList<T>> where T : SonarrObject, ISerializableNames<T>, new()
    {
        public override ReadOnlyList<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException($"Converter expected a JSON array but got {reader.TokenType}.");
            }

            List<T>? list = JsonSerializer.Deserialize<List<T>>(ref reader, options);

            return list is not null && list.Count > 0
                ? new ReadOnlyList<T>(list)
                : new ReadOnlyList<T>(Array.Empty<T>());
        }

        public override void Write(Utf8JsonWriter writer, ReadOnlyList<T> value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();
            foreach (T item in value)
            {
                writer.WriteRawValue(JsonSerializer.Serialize(item, item?.GetType() ?? typeof(T), options));
            }

            writer.WriteEndArray();
        }
    }
}

