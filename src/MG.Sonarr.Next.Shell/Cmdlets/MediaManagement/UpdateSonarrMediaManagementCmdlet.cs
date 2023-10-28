using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Models.Media;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.MediaManagement
{
    [Cmdlet(VerbsData.Update, "SonarrMediaManagement", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [MetadataCanPipe(Tag = Meta.MEDIA_MANGEMENT)]
    public sealed class UpdateSonarrMediaManagementCmdlet : SonarrApiCmdletBase
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [ValidateNotNull]
        public MediaManagementObject InputObject { get; set; } = null!;

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void Process(IServiceProvider provider)
        {
            var tag = provider.GetMetadataTag(Meta.MEDIA_MANGEMENT);
            string url = tag.GetUrlForId(this.InputObject.Id);

            if (!this.Force
                &&
                !this.ShouldProcess(url, "Update Media Management Settings"))
            {
                return;
            }

            if (!this.InputObject.IsRecycleBinPathProper(out string rbPath))
            {
                this.InputObject.RecycleBinPath = rbPath + '\\';
            }

            var response = this.SendPutRequest(url, this.InputObject);
            if (response.IsError)
            {
                this.InputObject.RecycleBinPath = rbPath;
                this.InputObject.Reset();
                this.WriteError(response.Error);
            }
            else
            {
                this.InputObject.Commit();
            }
        }
    }
}
