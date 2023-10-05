using MG.Sonarr.Next.Services.Models.Series;

namespace MG.Sonarr.Next.Services.Json.Converters
{
    public sealed class SeriesObjectConverter<T> : SonarrObjectConverter<T> where T : SeriesObject, new()
    {
        internal static readonly IReadOnlyDictionary<string, string> DeserializedNames =
            new Dictionary<string, string>(2, StringComparer.InvariantCultureIgnoreCase)
            {
                { "ImdbId", "IMDbId" },
                { "SeasonFolder", "UseSeasonFolders" },
            };

        internal static readonly IReadOnlyDictionary<string, string> SerializedNames =
            DeserializedNames.ToDictionary(x => x.Value, x => x.Key, StringComparer.InvariantCultureIgnoreCase);

        public SeriesObjectConverter(ObjectConverter converter)
            : base(converter)
        {
        }

        protected override IReadOnlyDictionary<string, string> GetDeserializedNames()
        {
            return DeserializedNames;
        }
        protected override IReadOnlyDictionary<string, string> GetSerializedNames()
        {
            return SerializedNames;
        }
    }
}
