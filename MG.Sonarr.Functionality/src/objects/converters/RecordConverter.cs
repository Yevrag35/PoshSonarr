using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MG.Sonarr.Functionality.Converters
{
    public class RecordConverter : JsonConverter<LogRecord>
    {
        public override LogRecord ReadJson(JsonReader reader, Type objectType, LogRecord existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jtok = JToken.ReadFrom(reader);
            if (jtok.SelectToken("$.exceptionType") != null)
                return jtok.ToObject<ExceptionLogRecord>();

            else
                return jtok.ToObject<LogRecord>();
        }
        public override void WriteJson(JsonWriter writer, LogRecord value, JsonSerializer serializer)
        {

        }
    }

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
