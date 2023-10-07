namespace MG.Sonarr.Next.Json
{
    public sealed class SonarrJsonOptions
    {
        readonly JsonSerializerOptions _deserializer;
        readonly JsonSerializerOptions _debugSerializer;
        readonly JsonSerializerOptions _requestSerializer;

        public SonarrJsonOptions(Action<JsonSerializerOptions> setupDeserializer)
        {
            _deserializer = new(JsonSerializerDefaults.Web);
            _requestSerializer = new(JsonSerializerDefaults.Web)
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            };

            _debugSerializer = new(JsonSerializerDefaults.Web)
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
            };

            setupDeserializer(_deserializer);

            foreach (var conv in _deserializer.Converters)
            {
                _requestSerializer.Converters.Add(conv);
                _debugSerializer.Converters.Add(conv);
            }
        }

        public JsonSerializerOptions GetForDeserializing()
        {
            return _deserializer;
        }
        public JsonSerializerOptions GetForDebugging()
        {
            return _debugSerializer;
        }
        public JsonSerializerOptions GetForSerializing()
        {
            return _requestSerializer;
        }
    }
}
