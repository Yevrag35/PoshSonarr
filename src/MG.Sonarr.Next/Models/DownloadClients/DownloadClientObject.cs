using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.DownloadClients
{
    [SonarrObject]
    public sealed class DownloadClientObject : TagUpdateObject<DownloadClientObject>,
        ISerializableNames<DownloadClientObject>,
        ITestPipeable
    {
        const int CAPACITY = 15;
        static readonly string _typeName = typeof(DownloadClientObject).GetTypeName();

        public string Name
        {
            get => this.GetValue<string>() ?? string.Empty;
            set => this.SetValue(value);
        }

        public DownloadClientObject()
            : base(CAPACITY)
        {
        }

        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.DOWNLOAD_CLIENT];
        }

        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }
    }
}
