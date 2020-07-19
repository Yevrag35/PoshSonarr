using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Converters
{
    public class CollectionConverter<T> : JsonConverter<List<T>>
    {
        public override List<T> ReadJson(JsonReader reader, Type objectType, List<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jtok = JToken.ReadFrom(reader);
            if (jtok != null)
            {
                return jtok.ToObject<List<T>>();
            }
            else
            {
                return new List<T>();
            }
        }
        public override void WriteJson(JsonWriter writer, List<T> value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }
}
