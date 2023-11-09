using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Qualities
{
    [SonarrObject]
    public sealed class QualityProfileObject : IdSonarrObject<QualityProfileObject>,
        ISerializableNames<QualityProfileObject>
    {
        const int CAPACITY = 10;
        static readonly string _typeName = typeof(QualityProfileObject).GetTypeName();

        public string Name { get; private set; } = string.Empty;

        public QualityProfileObject()
            : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.QUALITY_PROFILE];
        }

        protected override void OnDeserialized(bool alreadyCalled)
        {
            base.OnDeserialized(alreadyCalled);
            if (this.TryGetNonNullProperty(nameof(this.Name), out string? name))
            {
                this.Name = name;
            }
        }

        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}
