using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Profiles
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
