using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
            writer.WriteStartObject();
            if (value.IsError)
            {
                WriteAsError(writer, value.Error, value, options);
            }
            else
            {
                WriteAsSuccess(writer, value, options);
            }

            writer.WriteEndObject();
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
        private static void WriteAsSuccess(Utf8JsonWriter writer, ISonarrResponse value, JsonSerializerOptions options)
        {
            writer.WriteString(options.ConvertName(nameof(value.RequestUrl)), value.RequestUrl);
            writer.WriteNumber(options.ConvertName(nameof(value.StatusCode)), (int)value.StatusCode);
            writer.WriteString(options.ConvertName("Status"), value.StatusCode.ToString());
        }
    }
}
