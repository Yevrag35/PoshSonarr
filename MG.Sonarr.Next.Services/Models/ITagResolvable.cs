using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next.Services.Models
{
    public interface ITagResolvable<T> where T : IJsonMetadataTaggable
    {
        static abstract MetadataTag GetTag(MetadataResolver resolver);
    }
}
