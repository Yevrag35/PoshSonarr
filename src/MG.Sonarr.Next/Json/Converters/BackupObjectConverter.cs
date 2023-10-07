using MG.Sonarr.Next.Models.System;

namespace MG.Sonarr.Next.Json.Converters
{
    public sealed class BackupObjectConverter : SonarrObjectConverter<BackupObject>
    {
        internal static readonly IReadOnlyDictionary<string, string> DeserializedNames =
            new Dictionary<string, string>(2, StringComparer.InvariantCultureIgnoreCase)
            {
                { "Path", "BackupUri" },
            };

        internal static readonly IReadOnlyDictionary<string, string> SerializedNames =
            DeserializedNames.ToDictionary(x => x.Value, x => x.Key, StringComparer.InvariantCultureIgnoreCase);

        public BackupObjectConverter(ObjectConverter converter)
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
