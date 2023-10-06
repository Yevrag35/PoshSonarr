using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Services.Json.Converters
{
    public sealed class SonarrResponseConverter : JsonConverter<ISonarrResponse>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsAssignableTo(typeof(ISonarrResponse));
        }

        public override ISonarrResponse? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, ISonarrResponse value, JsonSerializerOptions options)
        {
            if (value.IsError)
            {
                writer.WriteStartObject();
                WriteAsError(writer, value.Error, value, options);
                writer.WriteEndObject();
            }
            else
            {
                WriteAsSuccess(writer, value, options);
            }
        }

        private static void WriteAsError(Utf8JsonWriter writer, ErrorRecord record, ISonarrResponse value, JsonSerializerOptions options)
        {
            Type errType = record.Exception.GetType();
            writer.WriteString(options.ConvertName(nameof(value.RequestUrl)), value.RequestUrl);
            writer.WriteNumber(options.ConvertName(nameof(value.StatusCode)), (int)value.StatusCode);
            writer.WriteString(options.ConvertName("Status"), value.StatusCode.ToString());
            writer.WriteString(options.ConvertName("ErrorType"), errType.FullName ?? errType.Name);
            writer.WriteString(options.ConvertName(nameof(Exception.Message)), record.Exception.GetBaseException().Message);
        }

        const string RECEIVED = "Received response -> ";
        private static void WriteAsSuccess(Utf8JsonWriter writer, ISonarrResponse value, JsonSerializerOptions options)
        {
            int length = RECEIVED.Length + 50;

            Span<char> span = stackalloc char[length];
            RECEIVED.CopyTo(span);
            int position = RECEIVED.Length;

            ((int)value.StatusCode).TryFormat(span.Slice(position), out int written);
            position += written;

            span[position++] = ' ';
            span[position++] = '(';

            ReadOnlySpan<char> v = value.StatusCode.ToString();
            v.CopyTo(span.Slice(position));
            position += v.Length;

            span[position++] = ')';

            writer.WriteRawValue(span.Slice(0, position), skipInputValidation: true);
        }
    }
}
