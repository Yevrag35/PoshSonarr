using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;

namespace MG.Sonarr.Functionality.Converters
{
    public class PathConverter : JsonConverter<string>
    {
        public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (hasExistingValue)
                return Path.GetDirectoryName(existingValue);
            
            else
                return existingValue;
        }
        public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer)
        {
            writer.WriteValue(value + "\\");
        }
    }
}
