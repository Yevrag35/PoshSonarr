using MG.Sonarr.Next.Services.Metadata;
using System.Text.Json.Serialization;

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
