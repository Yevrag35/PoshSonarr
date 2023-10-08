using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Profiles
{
    public sealed class ReleaseProfileObject : TagUpdateObject, IComparable<ReleaseProfileObject>
    {
        public string Name
        {
            get => this.GetValue<string>() ?? string.Empty;

        }

        public ReleaseProfileObject()
            : base(10)
        {
        }

        public int CompareTo(ReleaseProfileObject? other)
        {
            return this.CompareTo((TagUpdateObject?)other);
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.RELEASE_PROFILE];
        }
    }
}
