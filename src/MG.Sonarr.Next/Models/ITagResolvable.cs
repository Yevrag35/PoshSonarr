using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models
{
    public interface ITagResolvable<T> where T : IJsonMetadataTaggable
    {
        static abstract MetadataTag GetTag(IMetadataResolver resolver);
    }
}
