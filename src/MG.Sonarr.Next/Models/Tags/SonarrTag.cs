using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Tags
{
    public sealed record SonarrTag : IHasId, IJsonSonarrMetadata
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public required int Id { get; init; }
        public required string Label { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public MetadataTag MetadataTag { get; private set; } = MetadataTag.Empty;

        public void SetTag(IMetadataResolver resolver)
        {
            this.MetadataTag = resolver[Meta.TAG];
        }
    }
}
