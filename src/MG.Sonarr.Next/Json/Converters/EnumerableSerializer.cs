using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Json.Converters
{
    public sealed class EnumerableSerializer<T> : JsonConverter<IEnumerable<T>> where T : notnull
    {
        public override IEnumerable<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, IEnumerable<T> value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

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
