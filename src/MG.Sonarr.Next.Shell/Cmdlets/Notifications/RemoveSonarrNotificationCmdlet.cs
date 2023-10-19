using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Models.Notifications;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Notifications
{
    [Cmdlet(VerbsCommon.Remove, "SonarrNotification", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true,
        DefaultParameterSetName = "ByExplicitId")]
    [MetadataCanPipe(Tag = Meta.NOTIFICATION)]
    public sealed class RemoveSonarrNotificationCmdlet : SonarrApiCmdletBase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByPipelineInput")]
        public NotificationObject InputObject
        {
            get => null!;
            set => this.Id = value?.Id ?? 0;
        }

        [Parameter(Mandatory = true, ParameterSetName = "ByExplicitId")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Id { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void Process(IServiceProvider provider)
        {
            var tag = provider.GetMetadataTag(Meta.NOTIFICATION);

            if (this.Id <= 0)
            {
                return;
            }

            string url = tag.GetUrlForId(this.Id);
            if (!this.Force
                &&
                !this.ShouldProcess(url, $"Deleting Notification {this.Id}"))
            {
                return;
            }

            var response = this.SendDeleteRequest(url);
            if (response.IsError)
            {
                this.WriteError(response.Error);
            }
        }
    }
}
