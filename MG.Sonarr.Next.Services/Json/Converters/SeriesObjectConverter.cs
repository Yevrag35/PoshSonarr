using MG.Sonarr.Next.Services.Models.Series;

namespace MG.Sonarr.Next.Services.Json.Converters
{
    public sealed class SeriesObjectConverter : SonarrObjectConverter<SeriesObject>
    {
        public SeriesObjectConverter(ObjectConverter converter)
            : base(converter)
        {
        }

        protected override IReadOnlyDictionary<string, string> GetDeserializedNames()
        {
            Dictionary<string, string> dict = new(1, StringComparer.InvariantCultureIgnoreCase);
            foreach (var kvp in YieldReplacements())
            {
                dict.Add(kvp.Key, kvp.Value);
            }

            return dict;
        }
        protected override IReadOnlyDictionary<string, string> GetSerializedNames()
        {
            Dictionary<string, string> dict = new(1, StringComparer.InvariantCultureIgnoreCase);
            foreach (var kvp in YieldReplacements())
            {
                dict.Add(kvp.Value, kvp.Key);
            }

            return dict;
        }

        private static IEnumerable<KeyValuePair<string, string>> YieldReplacements()
        {
            yield return new("Monitored", "IsMonitored");
        }
    }
}
