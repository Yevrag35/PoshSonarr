using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality.Converters
{
    public class CalendarSeriesConverter : JsonConverter<Dictionary<string, object>>
    {
        internal static readonly string[] Properties = new string[2]
        {
            "seriesType", "title"
        };

        public override Dictionary<string, object> ReadJson(JsonReader reader, Type objectType, Dictionary<string, object> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jtok = JToken.ReadFrom(reader);
            var dict = new Dictionary<string, object>(SonarrFactory.NewIgnoreCase());

            if (jtok != null && jtok.Type == JTokenType.Object && jtok is JObject job)
            {
                foreach (JProperty jprop in job.Properties().Where(x => Properties.Contains(x.Name)))
                {
                    JToken token = jprop.Value;
                    object val = token != null ? token.ToObject<object>() : null;
                    dict.Add(jprop.Name, val);
                }
            }

            return dict;
        }
        public override void WriteJson(JsonWriter writer, Dictionary<string, object> value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
