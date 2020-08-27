using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Converters
{
    public class ReadOnlySetConverter<TValue> : JsonConverter<IReadOnlySet<TValue>>
    {
        public override IReadOnlySet<TValue> ReadJson(JsonReader reader, Type objectType, IReadOnlySet<TValue> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<ReadOnlySet<TValue>>(reader);
        }
        public override void WriteJson(JsonWriter writer, IReadOnlySet<TValue> value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
