using MG.Sonarr.Next.Models;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Json.Converters
{
    public class SonarrObjectConverter<T> : JsonConverter<T> where T : SonarrObject, ISerializableNames<T>, new()
    {
        readonly ObjectConverter _converter;
        readonly IReadOnlyDictionary<string, string> _deserializedNames;
        readonly IReadOnlyDictionary<string, string> _serializedNames;

        public SonarrObjectConverter(ObjectConverter converter)
        {
            _converter = converter;
            _deserializedNames = T.GetDeserializedNames();
            _serializedNames = T.GetSerializedNames();
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Converter expected a JSON object but got {reader.TokenType}.");
            }

            T obj = _converter.ConvertToObject<T>(ref reader, options, _deserializedNames);
            obj.OnDeserialized();
            return obj;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value is IJsonOnSerializing serializing)
            {
                serializing.OnSerializing();
            }

            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            _converter.WritePSObject(writer, options, value, _serializedNames);
        }
    }
}
