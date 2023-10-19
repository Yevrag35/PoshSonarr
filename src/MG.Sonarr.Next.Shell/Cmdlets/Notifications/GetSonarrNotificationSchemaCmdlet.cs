using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Notifications;

namespace MG.Sonarr.Next.Shell.Cmdlets.Notifications
{
    [Cmdlet(VerbsCommon.Get, "SonarrNotificationSchema")]
    public sealed class GetSonarrNotificationSchemaCmdlet : SonarrApiCmdletBase
    {
        protected override void Process(IServiceProvider provider)
        {
            var response = this.SendGetRequest<MetadataList<NotificationSchemaObject>>(Constants.NOTIFICATION_SCHEMA);
            _ = this.TryWriteObject(in response);
        }
    }
}
