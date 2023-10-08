using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Qualities
{
    public sealed class QualityProfileObject : SonarrObject
    {
        const int CAPACITY = 10;

        public int Id { get; private set; }
        public string Name { get; private set; } = string.Empty;

        public QualityProfileObject()
            : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.QUALITY_PROFILE];
        }

        public override void OnDeserialized()
        {
            if (this.TryGetId(out int id))
            {
                this.Id = id;
            }

            if (this.TryGetNonNullProperty(nameof(this.Name), out string? name))
            {
                this.Name = name;
            }
        }
    }
}
