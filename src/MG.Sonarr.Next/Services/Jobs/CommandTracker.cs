using MG.Sonarr.Next.Models.Commands;
using MG.Sonarr.Next.Models.Renames;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Jobs;

/// <summary>
/// Defines operations that submit Sonarr commands and track successful submissions.
/// </summary>
public interface ICommandTracker
{
	/// <summary>
	/// Sends a rename command to Sonarr.
	/// </summary>
	/// <param name="rename">The rename payload to submit.</param>
	/// <param name="token">The cancellation token for the request.</param>
	/// <returns>The result of the submitted rename command.</returns>
	SonarrClientResult<CommandObject> SendRename(PostRename rename, CancellationToken token = default);

	/// <summary>
	/// Sends an RSS sync command to Sonarr.
	/// </summary>
	/// <param name="priority">The command priority to request.</param>
	/// <param name="updateScheduledTask">A value that indicates whether the RSS scheduled task is updated.</param>
	/// <param name="token">The cancellation token for the request.</param>
	/// <returns>The result of the submitted RSS sync command.</returns>
	SonarrClientResult<CommandObject> SendRssSync(CommandPriority priority, bool updateScheduledTask, CancellationToken token = default);
}

/// <summary>
/// Represents the default command tracker implementation.
/// </summary>
file sealed class CommandTracker : ICommandTracker
{
	readonly ISonarrClient _client;
	readonly ICommandHistory _history;

	/// <summary>
	/// Initializes a new <see cref="CommandTracker"/> instance with the specified client and history store.
	/// </summary>
	/// <param name="client">The Sonarr client used to submit commands.</param>
	/// <param name="history">The command history that receives successful command results.</param>
	public CommandTracker(ISonarrClient client, ICommandHistory history)
	{
		_client = client;
		_history = history;
	}

	/// <inheritdoc cref="ICommandTracker.SendRename(PostRename, CancellationToken)"/>
	public SonarrClientResult<CommandObject> SendRename(PostRename rename, CancellationToken token = default)
	{
		var response = _client.SendPost<PostRename, CommandObject>(Constants.COMMAND, rename, token);
		if (!response.IsError)
		{
			_history.Add(response.Value);
		}

		return response;
	}

	/// <inheritdoc cref="ICommandTracker.SendRssSync(CommandPriority, bool, CancellationToken)"/>
	public SonarrClientResult<CommandObject> SendRssSync(CommandPriority priority, bool updateScheduledTask, CancellationToken token = default)
	{
		if (!Enum.IsDefined(priority))
		{
			priority = default;
		}

		PostCommand post = new()
		{
			CommandName = CommandStrings.COMMAND_RSS_SYNC,
			Name = CommandStrings.RSS_SYNC,
			Priority = priority,
			UpdateScheduledTask = updateScheduledTask,
		};

		var response = _client.SendPost<PostCommand, CommandObject>(Constants.COMMAND, post, token);

		if (!response.IsError)
		{
			_history.Add(response.Value);
		}

		return response;
	}
}

/// <summary>
/// Provides dependency injection registrations for command tracker services.
/// </summary>
public static class CommandTrackerDependencyInjection
{
	/// <summary>
	/// Adds command tracker services to the specified service collection.
	/// </summary>
	/// <param name="services">The service collection to update.</param>
	/// <returns>The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
	public static IServiceCollection AddCommandTracker(this IServiceCollection services)
	{
		return services
			.AddCommandHistory()
			.AddScoped<ICommandTracker, CommandTracker>();
	}
}
