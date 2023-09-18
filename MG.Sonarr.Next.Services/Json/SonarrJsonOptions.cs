namespace MG.Sonarr.Next.Services.Json
{
    public sealed class SonarrJsonOptions
    {
        readonly JsonSerializerOptions _deserializer;
        readonly JsonSerializerOptions _serializer;

        public SonarrJsonOptions(Action<JsonSerializerOptions> setupDeserializer)
        {
            _deserializer = new(JsonSerializerDefaults.Web);
            _serializer = new(JsonSerializerDefaults.Web);
            _serializer.WriteIndented = true;
            setupDeserializer(_deserializer);

            foreach (var conv in _deserializer.Converters)
            {
                _serializer.Converters.Add(conv);
            }
        }

        public JsonSerializerOptions GetForDeserializing()
        {
            return _deserializer;
        }
        public JsonSerializerOptions GetForSerializing()
        {
            return _serializer;
        }
    }
}
