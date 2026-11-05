using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Models.Notifications;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Notifications;

[Cmdlet(VerbsCommon.Remove, "SonarrNotification", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true,
	DefaultParameterSetName = PSConstants.PSetExplicitId)]
[MetadataCanPipe(Tag = Meta.Notification)]
public sealed class RemoveSonarrNotificationCmdlet : SonarrApiCmdletBase
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	[ValidateId(ValidateRangeKind.Positive)]
	public NotificationObject InputObject
	{
		get => null!;
		set => this.Id = value?.Id ?? 0;
	}

	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int Id { get; set; }

	[Parameter]
	public SwitchParameter Force { get; set; }

	protected override void Process(IServiceProvider provider)
	{
		var tag = provider.GetMetadataTag(Meta.Notification);

		if (this.Id <= 0)
		{
			return;
		}

		string url = tag.GetUrlForId(this.Id);
		if (!this.ShouldProcessWithForce(url, $"Deleting Notification {this.Id}", this.Force))
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
