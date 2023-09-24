using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next.Services.Models.Tags
{
    public sealed class TagObject : SonarrObject
    {
        public int Id { get; private set; }
        public string Label { get; private set; }

        public TagObject()
            : base(3)
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
