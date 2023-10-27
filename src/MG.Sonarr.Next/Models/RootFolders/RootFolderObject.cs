using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.RootFolders
{
    [SonarrObject]
    public sealed class RootFolderObject : IdSonarrObject<RootFolderObject>,
        ISerializableNames<RootFolderObject>
    {
        const int CAPACITY = 9;
        static readonly string _typeName = typeof(RootFolderObject).GetTypeName();

        public RootFolderObject()
            : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.ROOT_FOLDER];
        }

        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}
