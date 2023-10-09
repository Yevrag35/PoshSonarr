using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Commands;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Services.Jobs;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Commands
{
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
            if (this.HasParameter(x => x.Refresh, false))
            {
                commands = this.RefreshUnfinished(provider, history);
            }
            else if (!this.HasParameter(x => x.All, false))
            {
                commands = history.Where(x => !x.IsCompleted);
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
            var tag = provider.GetRequiredService<MetadataResolver>()[Meta.COMMAND];

            foreach (ICommand command in history.Where(x => !x.IsCompleted))
            {
                SonarrResponse<CommandObject> response = client.SendGet<CommandObject>(tag.GetUrlForId(command.Id));
                if (response.IsError)
                {
                    this.WriteConditionalError(response.Error);
                }
                else if (response.Data.IsCompleted)
                {
                    history[response.Data.Id] = response.Data;
                    yield return response.Data;
                }
            }
        }
        private void WriteResults(IEnumerable<ICommand> commands)
        {
            this.WriteCollection(commands.OrderBy(x => x.Started));
        }
    }
}
