using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Config
{
    [SonarrObject]
    public sealed class DownloadClientConfigObject : IdSonarrObject<DownloadClientConfigObject>,
        IJsonOnSerializing,
        ISerializableNames<DownloadClientConfigObject>
    {
        static readonly string _typeName = typeof(DownloadClientConfigObject).GetTypeName();
        const int CAPACITY = 5;

        public DownloadClientConfigObject()
            : base(CAPACITY)
        {
        }
        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.DOWNLOAD_CLIENT_CONFIG];
        }

        public void OnSerializing()
        {
            this.UpdateProperty(x => x.Id);
        }

        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}
