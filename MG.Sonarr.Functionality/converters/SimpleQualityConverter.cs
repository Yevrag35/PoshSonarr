using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality.Converters
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SimpleQualityConverter : JsonConverter<(int, string)>
    {
        public override (int, string) ReadJson(JsonReader reader, Type objectType, (int, string) existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jtok = JToken.ReadFrom(reader);
            (int, string) tuple = default;
            if (jtok != null && jtok.Type == JTokenType.Object && jtok is JObject job)
            {
                string qualityName = job.SelectToken("$.quality.name")?.ToObject<string>();
                int qualityId = job.SelectToken("$.quality.id").ToObject<int>();
                tuple = (qualityId, qualityName);
            }
            return tuple;
        }
        public override void WriteJson(JsonWriter writer, (int, string) value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("id");
            writer.WriteValue(value.Item1);
            writer.WritePropertyName("name");
            writer.WriteValue(value.Item2);
            writer.WriteEndObject();
        }
    }
}