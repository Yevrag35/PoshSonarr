using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Models.Commands;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Json.Converters
{
    public sealed class PostCommandWriter : JsonConverter<PostCommand>
    {
        public override PostCommand? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        const string SEND_UPDATES = "SendUpdatesToClient";
        const string TRIGGER = "Trigger";
        const string MANUAL = "manual";
        public override void Write(Utf8JsonWriter writer, PostCommand value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteStartObject();

            if (value is null)
            {
                writer.WriteEndObject();
                return;
            }

            writer.WriteString(options.ConvertName(nameof(value.Name)), value.Name);
            writer.WriteString(options.ConvertName(nameof(value.CommandName)), value.CommandName);

            writer.WriteBoolean(options.ConvertName(SEND_UPDATES), true);
            writer.WriteBoolean(options.ConvertName(nameof(value.UpdateScheduledTask)), value.UpdateScheduledTask);
            writer.WriteString(options.ConvertName(TRIGGER), MANUAL);

            writer.WritePropertyName(options.ConvertName(nameof(value.Priority)));

            ReadOnlySpan<char> asSpan = value.Priority.ToString();
            Span<char> priSpan = stackalloc char[asSpan.Length];
            asSpan.ToLower(priSpan, Statics.DefaultCulture);

            writer.WriteStringValue(priSpan);
            writer.WriteEndObject();
        }
    }
}
