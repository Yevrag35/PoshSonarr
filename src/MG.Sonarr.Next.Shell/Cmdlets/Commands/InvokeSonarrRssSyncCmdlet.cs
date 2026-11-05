using MG.Sonarr.Next.Models.Commands;
using MG.Sonarr.Next.Services.Jobs;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.Commands;

/// <summary>
/// Represents the <c>Invoke-SonarrRssSync</c> cmdlet, which starts Sonarr's command that syncs the RSS feeds of its
/// indexers.
/// </summary>
/// <remarks>
/// The cmdlet posts the command to Sonarr and writes the command that Sonarr queued to the pipeline as a
/// <see cref="CommandObject"/>, which the command history also keeps, so <c>Get-SonarrCommand</c> and
/// <c>Get-SonarrCommandHistory</c> can follow it. When the request fails, the cmdlet writes its error, unless it's
/// ignorable.
/// </remarks>
[Cmdlet(VerbsLifecycle.Invoke, "SonarrRssSync", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
public sealed class InvokeSonarrRssSyncCmdlet : TimedCmdlet
{
	/// <summary>
	/// Gets or sets the priority of the command in Sonarr's queue.
	/// </summary>
	/// <remarks>
	/// A value that <see cref="CommandPriority"/> doesn't define is sent as the default priority.
	/// </remarks>
	/// <value>The command's priority. The default is <see cref="CommandPriority.Normal"/>.</value>
	[Parameter]
	public CommandPriority Priority { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether Sonarr records this run as the last run of its scheduled RSS sync
	/// task.
	/// </summary>
	/// <value>The switch, which is present to update the scheduled task.</value>
	[Parameter]
	public SwitchParameter UpdateScheduledTask { get; set; }

	/// <summary>
	/// Starts the cmdlet's timer, sends the RSS sync command, and writes the command that Sonarr queued.
	/// </summary>
	/// <remarks>
	/// The command goes out through the scope's command tracker rather than a <c>Send*Request</c> helper, so the cmdlet
	/// starts its timer itself, and the verbose line after the response reports the request's time.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is null.</exception>
	protected override void Process(IServiceProvider provider)
	{
		ArgumentNullException.ThrowIfNull(provider);
		var tracker = provider.GetRequiredService<ICommandTracker>();

		this.StartTimer();
		var response = tracker.SendRssSync(this.Priority, this.UpdateScheduledTask.ToBool());
		bool written = this.TryWriteObject(response);
	}
}
