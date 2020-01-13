using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MG.Sonarr.Functionality.DateFormats
{
    public enum WeekColumnHeader
    {
        [DateFormat("ddd M/D")]
        Tue_3_25,

        [DateFormat("ddd MM/DD")]
        Tue_03_25,

        [DateFormat("ddd D/M")]
        Tue_25_3,

        [DateFormat("ddd DD/MM")]
        Tue_25_03
    }

    public class WeekColumnHeaderConverter : JsonConverter<WeekColumnHeader>
    {
        public override WeekColumnHeader ReadJson(JsonReader reader, Type objectType, WeekColumnHeader existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                string dfStr = reader.Value as string;
                if (!string.IsNullOrEmpty(dfStr) && DateFormatAttribute.TryGetFormatFromString(dfStr, out WeekColumnHeader outDf))
                {
                    return outDf;
                }
                else
                    return default;
            }
            else
                return default;
        }
        public override void WriteJson(JsonWriter writer, WeekColumnHeader value, JsonSerializer serializer)
        {
            writer.WriteValue(DateFormatAttribute.GetStringFromFormat(value));
        }
    }
}
