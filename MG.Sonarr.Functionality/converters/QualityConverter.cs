using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;

namespace MG.Sonarr.Functionality.Converters
{
    internal class QualityConverter : JsonConverter<IQuality>
    {
        public override IQuality ReadJson(JsonReader reader, Type objectType, IQuality existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<Quality>(reader);
        }
        public override void WriteJson(JsonWriter writer, IQuality value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
