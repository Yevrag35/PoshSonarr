using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality.Converters
{
    public class HistoryDataConverter : JsonConverter<Dictionary<string, string>>
    {
        public override Dictionary<string, string> ReadJson(JsonReader reader, Type objectType, Dictionary<string, string> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jtok = JToken.ReadFrom(reader);
            var dict = new Dictionary<string, string>(3, SonarrFactory.NewIgnoreCase());

            if (jtok != null && jtok.Type == JTokenType.Object && jtok is JObject job)
            {
                foreach (JProperty jprop in job.Properties())
                {
                    JToken jval = jprop.Value;
                    dict.Add(jprop.Name, jval?.ToObject<string>());
                }
            }
            return dict;
        }
        public override void WriteJson(JsonWriter writer, Dictionary<string, string> value, JsonSerializer serializer)
        {
        }
    }
}