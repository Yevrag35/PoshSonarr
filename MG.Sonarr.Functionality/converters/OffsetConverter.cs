using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;

namespace MG.Sonarr.Functionality.Converters
{
    public class OffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset ReadJson(JsonReader reader, Type objectType, DateTimeOffset existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jtok = JToken.ReadFrom(reader);
            if (jtok != null && jtok.Type == JTokenType.Date)
            {
                DateTime readDate = jtok.ToObject<DateTime>();
                DateTime utcDate = DateTime.SpecifyKind(readDate, DateTimeKind.Utc);
                return new DateTimeOffset(utcDate);
            }
            else
            {
                return existingValue;
            }
        }
        public override void WriteJson(JsonWriter writer, DateTimeOffset value, JsonSerializer serializer)
        {
            writer.WriteValue(value.UtcDateTime);
        }
    }
}
