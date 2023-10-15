using MG.Sonarr.Functionality.Extensions;
using MG.Sonarr.Functionality.Strings;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality.Converters
{
    public class TermStoreConverter : JsonConverter<JsonStringSet>
    {
        

        public override JsonStringSet ReadJson(JsonReader reader, Type objectType, JsonStringSet existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jtok = JToken.ReadFrom(reader);
            if (jtok.Type == JTokenType.String)
            {
                string termStore = jtok.ToObject<string>();
                if (!string.IsNullOrWhiteSpace(termStore))
                {
                    return new JsonStringSet(termStore);
                }
            }
            return new JsonStringSet();
        }
        public override void WriteJson(JsonWriter writer, JsonStringSet value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToJson());
        }
    }
}
