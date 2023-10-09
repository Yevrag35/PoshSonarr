using MG.Sonarr.Next.Models.Commands;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Jobs
{
    public interface ICommandTracker
    {
        SonarrResponse<CommandObject> SendRssSync(CommandPriority priority, bool updateScheduledTask, CancellationToken token = default);
    }

    file sealed class CommandTracker : ICommandTracker
    {
        readonly ISonarrClient _client;
        readonly ICommandHistory _history;

        public CommandTracker(ISonarrClient client, ICommandHistory history)
        {
            _client = client;
            _history = history;
        }

        public SonarrResponse<CommandObject> SendRssSync(CommandPriority priority, bool updateScheduledTask, CancellationToken token = default)
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

            if (!response.IsEmpty && !response.IsError)
            {
                _history.Add(response.Data);
            }

            return response;
        }
    }

    public static class CommandTrackerDependencyInjection
    {
        public static IServiceCollection AddCommandTracker(this IServiceCollection services)
        {
            return services
                .AddCommandHistory()
                .AddScoped<ICommandTracker, CommandTracker>();
        }
    }
}
