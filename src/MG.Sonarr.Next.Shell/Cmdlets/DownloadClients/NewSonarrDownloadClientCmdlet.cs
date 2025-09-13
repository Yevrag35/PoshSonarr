using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.DownloadClients;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.DownloadClients
{
    [Cmdlet(VerbsCommon.New, "SonarrDownloadClient", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [MetadataCanPipe(Tag = Meta.DOWNLOAD_CLIENT_SCHEMA)]
    public sealed class NewSonarrDownloadClientCmdlet : SonarrMetadataCmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public DownloadClientSchemaObject Schema { get; set; } = null!;

        [Parameter]
        public string? Name { get; set; }

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.DOWNLOAD_CLIENT];
        }
    }
}
