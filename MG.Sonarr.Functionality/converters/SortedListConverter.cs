using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Converters
{
    public class SortedListConverter<TCol, TKey, TVal> : JsonConverter<TCol> where TCol : SortedListBase<TKey, TVal>
    {
        public override TCol ReadJson(JsonReader reader, Type objectType, TCol existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<TCol>(reader);
        }
        public override void WriteJson(JsonWriter writer, TCol value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.GetValues());
        }
    }
}
