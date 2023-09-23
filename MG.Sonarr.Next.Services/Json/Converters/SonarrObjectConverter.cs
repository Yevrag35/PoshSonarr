using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Models;
using System.Buffers;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Text;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Services.Json.Converters
{
    public sealed class SonarrObjectConverter<T> : JsonConverter<T> where T : SonarrObject, new()
    {
        readonly ObjectConverter _converter;
        public SonarrObjectConverter(ObjectConverter converter)
        {
            _converter = converter;
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Converter expected a JSON object but got {reader.TokenType}.");
            }

            return _converter.ConvertToObject<T>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            _converter.WritePSObject(writer, options, value);
        }
    }
}
