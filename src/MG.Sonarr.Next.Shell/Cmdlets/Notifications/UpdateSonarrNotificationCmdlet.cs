using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Models.Notifications;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Notifications;

[Cmdlet(VerbsData.Update, "SonarrNotification", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
[MetadataCanPipe(Tag = Meta.Notification)]
public sealed class UpdateSonarrNotificationCmdlet : SonarrApiCmdletBase
{
	[Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public NotificationObject[] InputObject { get; set; } = Array.Empty<NotificationObject>();

	[Parameter]
	public SwitchParameter Force { get; set; }

	Dictionary<int, NotificationObject> _dict = null!;

	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_dict = [];
	}

	protected override void Process(IServiceProvider provider)
	{
		foreach (var obj in this.InputObject)
		{
			_ = _dict.TryAdd(obj.Id, obj);
		}
	}

	protected override void End(IServiceProvider provider)
	{
		var tag = provider.GetMetadataTag(Meta.Notification);
		bool yesToAll = false;
		bool noToAll = false;

		foreach (var notification in _dict.Values.OrderBy(x => x.Id))
		{
			string url = tag.GetUrlForId(notification.Id);
			if (!this.ShouldProcessWithForce(url, "Update Notification", this.Force))
			{
				continue;
			}

			if (!this.Force && !this.ShouldContinue(
				query: string.Format(CultureInfo.CurrentCulture, "Are you sure you want to update the notification: {0}", notification.Id),
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
