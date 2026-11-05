using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Commands;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Services.Jobs;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Commands;

[Cmdlet(VerbsCommon.Get, "SonarrCommandHistory", DefaultParameterSetName = "None")]
public sealed class GetSonarrCommandHistory : SonarrCmdletBase
{
	[Parameter(Mandatory = true, ParameterSetName = "ShowAll")]
	public SwitchParameter All { get; set; }

	[Parameter(Mandatory = true, ParameterSetName = "RefreshUnfinished")]
	public SwitchParameter Refresh { get; set; }

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
	private static bool IsUnfinished(ICommand command)
	{
		return !command.IsCompleted && !command.Ended.HasValue;
	}
	private void WriteResults(IEnumerable<ICommand> commands)
	{
		this.WriteCollection(commands.OrderBy(x => x.Started));
	}
}
