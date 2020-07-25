using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;

namespace MG.Sonarr.Functionality.Converters
{
    public class UtcTimeConverter : DateTimeConverterBase
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken jtok = JToken.ReadFrom(reader);
            if (jtok.Type == JTokenType.Date)
            {
                return jtok.ToObject<DateTime>().ToLocalTime();
            }
            else
                return jtok.ToObject<object>();
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is DateTime dt)
            {
                writer.WriteValue(JsonConvert.SerializeObject(dt, new JsonSerializerSettings
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc
                }));
            }
        }
    }
}
