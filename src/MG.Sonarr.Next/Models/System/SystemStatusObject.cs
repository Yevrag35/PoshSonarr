using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.System
{
    [SonarrObject]
    public sealed class SystemStatusObject : SonarrObject,
        ISerializableNames<SystemStatusObject>
    {
        const int CAPACITY = 26;
        static readonly string _typeName = typeof(SystemStatusObject).GetTypeName();

        public SystemStatusObject()
            : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.STATUS];
        }

        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}

