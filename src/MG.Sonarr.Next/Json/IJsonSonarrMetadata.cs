using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Json
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
