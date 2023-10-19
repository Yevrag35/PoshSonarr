using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Models.Notifications;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Notifications
{
    [Cmdlet(VerbsData.Update, "SonarrNotification", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [MetadataCanPipe(Tag = Meta.NOTIFICATION)]
    public sealed class UpdateSonarrNotificationCmdlet : SonarrApiCmdletBase
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public NotificationObject[] InputObject { get; set; } = Array.Empty<NotificationObject>();

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void Process(IServiceProvider provider)
        {
            var dict = provider.GetRequiredService<Dictionary<int, NotificationObject>>();
            foreach (var obj in this.InputObject)
            {
                _ = dict.TryAdd(obj.Id, obj);
            }
        }

        protected override void End(IServiceProvider provider)
        {
            var dict = provider.GetRequiredService<Dictionary<int, NotificationObject>>();
            var tag = provider.GetMetadataTag(Meta.NOTIFICATION);
            bool yesToAll = false;
            bool noToAll = false;

            foreach (var notification in dict.Values.OrderBy(x => x.Id))
            {
                string url = tag.GetUrlForId(notification.Id);
                if (!this.ShouldProcess(url, "Update Notification"))
                {
                    continue;
                }

                if (!this.Force && !this.ShouldContinue(
                    query: string.Format("Are you sure you want to update the notifcation: {0}", notification.Id),
                    caption: url,
                    ref yesToAll,
                    ref noToAll))
                {
                    continue;
                }

                var response = this.SendPutRequest(url, notification);
                if (response.IsError)
                {
                    notification.Reset();
                    this.WriteConditionalError(response.Error);
                }
                else
                {
                    notification.Commit();
                }
            }
        }
    }
}
