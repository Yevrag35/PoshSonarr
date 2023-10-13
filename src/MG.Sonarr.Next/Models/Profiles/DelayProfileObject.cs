using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Profiles
{
    [SonarrObject]
    public sealed class DelayProfileObject : TagUpdateObject<DelayProfileObject>,
        ISerializableNames<DelayProfileObject>
    {
        const int CAPACITY = 7;

        public DelayProfileObject()
            : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.DELAY_PROFILE];
        }
    }
}
