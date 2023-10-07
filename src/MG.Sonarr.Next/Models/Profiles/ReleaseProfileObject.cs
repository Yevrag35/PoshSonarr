using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Profiles
{
    public sealed class ReleaseProfileObject : TagUpdateObject
    {
        public string Name
        {
            get => this.GetValue<string>() ?? string.Empty;

        }

        public ReleaseProfileObject()
            : base(10)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.RELEASE_PROFILE];
        }
    }
}
