using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Commands;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Services.Jobs;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Commands;

/// <summary>
/// Represents the <c>Get-SonarrCommandHistory</c> cmdlet, which gets the commands that the module has started on the
/// current connection.
/// </summary>
/// <remarks>
/// <para>
/// The history holds the commands that cmdlets such as <c>Invoke-SonarrRssSync</c> and <c>Invoke-SonarrRename</c>
/// started since <c>Connect-SonarrInstance</c> made the connection, and the cmdlet writes them to the pipeline in the
/// order that they started. Without a switch, it writes the unfinished ones: those that haven't completed or ended.
/// <c>-All</c> writes every command in the history.
/// </para>
/// <para>
/// <c>-Refresh</c> gets each unfinished command from Sonarr again, stores its new state in the history, and writes the
/// ones that have finished since, whether they completed, failed, or were aborted. A failed request writes its error,
/// unless it's ignorable, and the cmdlet goes on to the next command. Those requests write no verbose or debug output.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrCommandHistory", DefaultParameterSetName = "None")]
public sealed class GetSonarrCommandHistory : SonarrCmdletBase
{
	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet writes every command in the history.
	/// </summary>
	/// <remarks>
	/// The switch can't be combined with <c>-Refresh</c>.
	/// </remarks>
	/// <value>The switch, which is present to write finished commands too.</value>
	[Parameter(Mandatory = true, ParameterSetName = "ShowAll")]
	public SwitchParameter All { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet gets the unfinished commands from Sonarr again and writes
	/// those that have finished.
	/// </summary>
	/// <remarks>
	/// The switch can't be combined with <c>-All</c>.
	/// </remarks>
	/// <value>The switch, which is present to refresh the unfinished commands.</value>
	[Parameter(Mandatory = true, ParameterSetName = "RefreshUnfinished")]
	public SwitchParameter Refresh { get; set; }

	/// <summary>
	/// Writes the commands that the switches select from the command history, in the order that they started.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		var history = provider.GetRequiredService<ICommandHistory>();

		IEnumerable<ICommand> commands;
		if (this.Refresh)
		{
			commands = this.RefreshUnfinished(provider, history);
		}
		else if (!this.All)
		{
			commands = history.Where(IsUnfinished);
		}
		else
		{
			commands = history;
		}

		this.WriteResults(commands);
	}

	/// <summary>
	/// Gets each unfinished command in the specified history from Sonarr, stores its new state in the history, and
	/// yields the commands that have finished.
	/// </summary>
	/// <remarks>
	/// The method is an iterator, so the requests go out as the caller enumerates it, and it enumerates a copy of the
	/// unfinished commands, since it changes the history. A failed request writes its error, unless it's ignorable, and
	/// leaves the command's entry as it was. The client comes from a scope without an API cmdlet, so the requests
	/// write no verbose or debug output.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	/// <param name="history">The session's command history.</param>
	/// <returns>The refreshed commands that have completed or ended.</returns>
	private IEnumerable<ICommand> RefreshUnfinished(IServiceProvider provider, ICommandHistory history)
	{
		var client = provider.GetRequiredService<ISonarrClient>();
		var tag = provider.GetRequiredService<IMetadataResolver>()[Meta.Command];

		foreach (ICommand command in history.Where(IsUnfinished).ToArray())
		{
			SonarrClientResult<CommandObject> response = client.SendGet<CommandObject>(tag.GetUrlForId(command.Id));
			if (response.IsError)
			{
				this.WriteConditionalError(response.Error);
				continue;
			}

			// The history keeps the latest state of every command that it refreshes, and a command that has ended, as
			// a failed or aborted one has, isn't unfinished, whether or not it completed.
			history[response.Value.Id] = response.Value;
			if (!IsUnfinished(response.Value))
			{
				yield return response.Value;
			}
		}
	}
	/// <summary>
	/// Determines whether the specified command is still queued or running.
	/// </summary>
	/// <param name="command">The command to check.</param>
	/// <returns>
	/// <see langword="true"/> if the command hasn't completed and has no end time; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool IsUnfinished(ICommand command)
	{
		return !command.IsCompleted && !command.Ended.HasValue;
	}
	/// <summary>
	/// Writes the specified commands to the pipeline, in the order that they started.
	/// </summary>
	/// <param name="commands">The commands to write.</param>
	private void WriteResults(IEnumerable<ICommand> commands)
	{
		this.WriteCollection(commands.OrderBy(x => x.Started));
	}
}
