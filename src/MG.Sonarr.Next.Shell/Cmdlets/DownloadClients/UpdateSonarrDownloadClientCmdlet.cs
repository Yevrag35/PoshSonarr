using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.DownloadClients;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.DownloadClients
{
    [Cmdlet(VerbsData.Update, "SonarrDownloadClient", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [MetadataCanPipe(Tag = Meta.DOWNLOAD_CLIENT)]
    public sealed class UpdateSonarrDownloadClientCmdlet : SonarrMetadataCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [ValidateIds(ValidateRangeKind.Positive)]
        public DownloadClientObject[] InputObject { get; set; } = Array.Empty<DownloadClientObject>();

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.DOWNLOAD_CLIENT];
        }

        protected override void Process(IServiceProvider provider)
        {
            foreach (DownloadClientObject dco in this.InputObject)
            {
                this.UpdateClient(dco, this.Tag);
            }
        }

        private void UpdateClient(DownloadClientObject client, MetadataTag tag)
        {
            string url = tag.GetUrlForId(client.Id);
            if (!this.ShouldProcess(url, "Update Download Client"))
            {
                return;
            }

            var response = this.SendPutRequest(url, client);
            _ = this.TryCommitFromResponse(client, in response);
        }
    }
}

