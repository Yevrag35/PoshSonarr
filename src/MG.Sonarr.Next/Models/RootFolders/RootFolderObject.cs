using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.RootFolders
{
    public sealed class RootFolderObject : SonarrObject
    {
        public int Id { get; private set; }

        public RootFolderObject()
            : base(9)
        {
        }

        protected override MetadataTag GetTag(MetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.ROOT_FOLDER];
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
