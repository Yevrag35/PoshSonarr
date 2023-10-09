using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Profiles
{
    public sealed class DelayProfileObject : TagUpdateObject,
        IComparable<DelayProfileObject>,
        ISerializableNames<DelayProfileObject>
    {
        public DelayProfileObject()
            : base(7)
        {
        }

        public int CompareTo(DelayProfileObject? other)
        {
            return this.CompareTo((TagUpdateObject?)other);
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.DELAY_PROFILE];
        }
    }
}
