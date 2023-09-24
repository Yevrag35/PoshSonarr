using MG.Sonarr.Next.Services.Collections;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Services.Json.Converters
{
    public class SonarrObjectConverter<T> : JsonConverter<T> where T : SonarrObject, new()
    {
        readonly ObjectConverter _converter;
        readonly IReadOnlyDictionary<string, string> _deserializedNames;
        readonly IReadOnlyDictionary<string, string> _serializedNames;
        readonly MetadataTag? _tag;

        public SonarrObjectConverter(ObjectConverter converter, MetadataTag? tag = null)
        {
            _converter = converter;
            _deserializedNames = this.GetDeserializedNames();
            _serializedNames = this.GetSerializedNames();
            _tag = tag;
        }

        protected virtual IReadOnlyDictionary<string, string> GetDeserializedNames()
        {
            return EmptyNameDictionary.Default;
        }
        protected virtual IReadOnlyDictionary<string, string> GetSerializedNames()
        {
            return EmptyNameDictionary.Default;
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
