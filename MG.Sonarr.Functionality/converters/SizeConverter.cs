using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Converters
{
    public class SizeConverter : JsonConverter<Size>
    {
        public override Size ReadJson(JsonReader reader, Type objectType, Size existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            Size newValue = default;
            JToken jtok = JToken.ReadFrom(reader);
            if (jtok.Type != JTokenType.Integer)
                return newValue;

            newValue = jtok.ToObject<long>();
            return newValue;
        }
        public override void WriteJson(JsonWriter writer, Size value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToJson());
        }
    }
}
