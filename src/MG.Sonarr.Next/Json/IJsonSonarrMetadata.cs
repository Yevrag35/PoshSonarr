using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next.Services.Json
{
    public interface IJsonMetadataTaggable
    {
        void SetTag(MetadataResolver resolver);
    }

    public interface IJsonSonarrMetadata : IJsonMetadataTaggable
    {
        MetadataTag MetadataTag { get; }
    }
}
