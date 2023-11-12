using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Profiles
{
    [SonarrObject]
    public sealed class ReleaseProfileObject : TagUpdateObject<ReleaseProfileObject>,
        ISerializableNames<ReleaseProfileObject>
    {
        static readonly string _typeName = typeof(ReleaseProfileObject).GetTypeName();

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

        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}
