using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.RootFolders
{
    public sealed class RootFolderObject : SonarrObject,
        IComparable<RootFolderObject>,
        ISerializableNames<RootFolderObject>
    {
        const int CAPACITY = 9;

        public int Id { get; private set; }

        public RootFolderObject()
            : base(CAPACITY)
        {
        }

        public int CompareTo(RootFolderObject? other)
        {
            return Comparer<int?>.Default.Compare(this.Id, other?.Id);
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
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
