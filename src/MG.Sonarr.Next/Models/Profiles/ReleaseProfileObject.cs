using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Profiles
{
    [SonarrObject]
    public sealed class ReleaseProfileObject : TagUpdateObject<ReleaseProfileObject>,
        ISerializableNames<ReleaseProfileObject>
    {
        public string Name
        {
            get => this.GetStringOrEmpty();
            set => this.SetValue(value);
        }

        public ReleaseProfileObject()
            : base(10)
        {
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.RELEASE_PROFILE];
        }
    }
}
