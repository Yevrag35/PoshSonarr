using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next.Services.Models.Tags
{
    public sealed class TagObject : SonarrObject
    {
        public TagObject()
            : base(3)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.TAG];
        }
    }
}
