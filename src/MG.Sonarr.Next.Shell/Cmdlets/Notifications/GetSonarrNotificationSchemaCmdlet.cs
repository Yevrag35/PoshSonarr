using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Notifications;

namespace MG.Sonarr.Next.Shell.Cmdlets.Notifications;

/// <summary>
/// Represents the <c>Get-SonarrNotificationSchema</c> cmdlet, which gets the templates that new notification
/// connections are created from.
/// </summary>
/// <remarks>
/// Sonarr returns a template for each kind of notification connection that it supports, and the cmdlet writes each one
/// to the pipeline as a <see cref="NotificationSchemaObject"/>.
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrNotificationSchema")]
public sealed class GetSonarrNotificationSchemaCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets every notification template and writes each one to the pipeline.
	/// </summary>
	/// <remarks>
	/// When the request fails, the cmdlet writes its error, unless the error is ignorable, and writes no templates.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		var response = this.SendGetRequest<MetadataList<NotificationSchemaObject>>(Constants.NotificationSchema);
		_ = this.TryWriteObject(response);
	}
}
