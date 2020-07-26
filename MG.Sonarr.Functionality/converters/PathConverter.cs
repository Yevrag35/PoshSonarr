using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;

namespace MG.Sonarr.Functionality.Converters
{
    public class PathConverter : JsonConverter<string>
    {
        public static readonly char BACKSLASH = char.Parse("\\");

        public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string val = null;
            if (reader.Value is string strPath)
            {
                val = strPath.TrimEnd(BACKSLASH);
            }
            return val;
        }
        public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer)
        {
            writer.WriteValue(value + "\\");
        }
    }
}
