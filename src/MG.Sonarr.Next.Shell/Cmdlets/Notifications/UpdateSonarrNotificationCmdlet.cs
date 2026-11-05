using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Models.Notifications;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Notifications;

/// <summary>
/// Represents the <c>Update-SonarrNotification</c> cmdlet, which saves changes made to notification connections back
/// to Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes <see cref="NotificationObject"/> instances, as <c>Get-SonarrNotification</c> writes them, from the
/// pipeline or by name. It collects them, keeping the first one for each ID, and once the input ends, it sends each one
/// to Sonarr, in ascending order of ID, replacing the connection that has its ID. It writes nothing to the pipeline.
/// </para>
/// <para>
/// The cmdlet confirms each connection through <c>ShouldProcess</c>, whose confirm impact is Low, so that prompt
/// appears only under <c>-Confirm</c> or a <c>$ConfirmPreference</c> of Low. Without <c>-Force</c>, it then asks
/// whether to update each connection, in a prompt that offers Yes to All and No to All. <c>-Force</c> skips both
/// prompts, but not <c>-WhatIf</c> or an explicit <c>-Confirm</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsData.Update, "SonarrNotification", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
[MetadataCanPipe(Tag = Meta.Notification)]
public sealed class UpdateSonarrNotificationCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets or sets the notification connections to save.
	/// </summary>
	/// <remarks>
	/// The parameter takes the connections from the pipeline, and each one's ID has to be greater than zero. Of the
	/// connections that share an ID, the cmdlet sends the first.
	/// </remarks>
	/// <value>The connections, with the changes to send. The default is an empty array.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public NotificationObject[] InputObject { get; set; } = Array.Empty<NotificationObject>();

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet updates the notification connections without asking
	/// about each one.
	/// </summary>
	/// <remarks>
	/// The switch skips the confirmation prompt and the cmdlet's own prompt for each connection. In What if mode the
	/// cmdlet still sends nothing, and an explicit <c>-Confirm</c> still prompts.
	/// </remarks>
	/// <value>The switch, which is present to skip the prompts.</value>
	[Parameter]
	public SwitchParameter Force { get; set; }

	Dictionary<int, NotificationObject> _dict = null!;

	/// <summary>
	/// Runs the base class's setup for the scope, then creates the dictionary of connections that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_dict = [];
	}

	/// <summary>
	/// Adds each notification connection from <c>-InputObject</c> to the ones to send, unless one with its ID is there
	/// already.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		foreach (var obj in this.InputObject)
		{
			_ = _dict.TryAdd(obj.Id, obj);
		}
	}

	/// <summary>
	/// Sends each of the collected notification connections to Sonarr, in ascending order of ID, once the action is
	/// confirmed, and commits or resets its properties as the request succeeds or fails.
	/// </summary>
	/// <remarks>
	/// A connection that either prompt turns down is skipped. When a request fails, the cmdlet writes its error, unless
	/// the error is ignorable, and goes on to the next connection.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
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
