using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.DownloadClients;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.DownloadClients
{
    [Cmdlet(VerbsCommon.Get, "SonarrDownloadClient")]
    public sealed class GetSonarrDownloadClientCmdlet : SonarrMetadataCmdlet
    {
        const int CAPACITY = 2;
        protected override int Capacity => CAPACITY;

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
        }
        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.DOWNLOAD_CLIENT];
        }

        protected override void Process(IServiceProvider provider)
        {
            var all = this.GetAll<DownloadClientObject>();
            this.WriteCollection(all);
        }
    }
}
