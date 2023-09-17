namespace MG.Sonarr.Next.Services.Json
{
    public sealed class SonarrJsonOptions
    {
        readonly JsonSerializerOptions _options;

        public SonarrJsonOptions(Action<JsonSerializerOptions> setup)
        {
            _options = new(JsonSerializerDefaults.Web);
            setup(_options);
        }

        public JsonSerializerOptions GetOptions()
        {
            return _options;
        }
    }
}
