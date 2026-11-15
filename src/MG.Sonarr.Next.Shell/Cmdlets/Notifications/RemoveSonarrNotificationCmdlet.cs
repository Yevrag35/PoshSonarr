using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Models.Notifications;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Notifications;

/// <summary>
/// Represents the <c>Remove-SonarrNotification</c> cmdlet, which deletes notification connections from Sonarr.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes a connection's ID from <c>-Id</c>, or <see cref="NotificationObject"/> instances from the
/// pipeline, as <c>Get-SonarrNotification</c> writes them, and sends a request that deletes each connection as it
/// arrives. It writes nothing to the pipeline.
/// </para>
/// <para>
/// Its confirm impact is High, so it confirms each connection before it deletes it. <c>-Force</c> skips that prompt,
/// but not <c>-WhatIf</c> or an explicit <c>-Confirm</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Remove, "SonarrNotification", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true,
	DefaultParameterSetName = PSConstants.PSetExplicitId)]
[MetadataCanPipe(Tag = Meta.Notification)]
public sealed class RemoveSonarrNotificationCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets or sets the notification connection to delete.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The parameter takes the connection from the pipeline, and its ID has to be greater than zero. It can't be
	/// combined with <c>-Id</c>.
	/// </para>
	/// <para>
	/// Setting the property sets <see cref="Id"/> to the connection's ID, and the property keeps nothing else.
	/// </para>
	/// </remarks>
	/// <value>
	/// The connection, as <c>Get-SonarrNotification</c> writes it. A read always gives <see langword="null"/>.
	/// </value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	[ValidateId(ValidateRangeKind.Positive)]
	public NotificationObject InputObject
	{
		get => null!;
		set => this.Id = value?.Id ?? 0;
	}

	/// <summary>
	/// Gets or sets the ID of the notification connection to delete.
	/// </summary>
	/// <remarks>
	/// The ID has to be greater than zero. The parameter can't be combined with <c>-InputObject</c>.
	/// </remarks>
	/// <value>The connection's ID, which <c>-InputObject</c> also sets. The default is 0.</value>
	[Parameter(Mandatory = true, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet deletes the notification connections without confirming
	/// each one.
	/// </summary>
	/// <remarks>
	/// The switch skips the confirmation prompt only: in What if mode the cmdlet still deletes nothing, and an explicit
	/// <c>-Confirm</c> still prompts.
	/// </remarks>
	/// <value>The switch, which is present to skip the confirmation prompt.</value>
	[Parameter]
	public SwitchParameter Force { get; set; }

	/// <summary>
	/// Deletes the notification connection with the ID once the action is confirmed.
	/// </summary>
	/// <remarks>
	/// The cmdlet sends nothing when the ID isn't greater than zero. When the request fails, it writes the request's
	/// error, whatever the error is.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
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
