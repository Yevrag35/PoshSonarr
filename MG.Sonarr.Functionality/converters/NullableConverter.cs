using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;

namespace MG.Sonarr.Functionality.Converters
{
    public class NullableLongConverter : JsonConverter<long?>
    {
        public override long? ReadJson(JsonReader reader, Type objectType, long? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jtok = JToken.ReadFrom(reader);
            if (jtok != null && jtok.Type == JTokenType.Integer)
            {
                long number = jtok.ToObject<long>();
                return number != 0 ? number : (long?)null;
            }
            else
            {
                return null;
            }
        }
        public override void WriteJson(JsonWriter writer, long? value, JsonSerializer serializer)
        {
            writer.WriteValue(value.GetValueOrDefault());
        }
    }
}
