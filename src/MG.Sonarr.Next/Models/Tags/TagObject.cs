using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Tags
{
    public sealed class TagObject : SonarrObject
    {
        const int CAPACITY = 3;

        public int Id { get; private set; }
        public string Label { get; private set; } = string.Empty;

        public TagObject()
            : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.TAG];
        }

        public override void OnDeserialized()
        {
            if (this.TryGetId(out int id))
            {
                this.Id = id;
            }

            if (this.TryGetProperty(Constants.LABEL, out string? label))
            {
                this.Label = label ?? string.Empty;
            }
        }
    }
}
