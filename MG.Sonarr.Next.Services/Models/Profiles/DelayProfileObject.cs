using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next.Services.Models.Profiles
{
    public sealed class DelayProfileObject : TagUpdateObject
    {
        public DelayProfileObject()
            : base(7)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.DELAY_PROFILE];
        }
    }
}
