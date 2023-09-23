using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next.Services.Models.Tags
{
    public sealed class TagObject : SonarrObject
    {
        public int Id { get; private set; }

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
        }
    }
}
