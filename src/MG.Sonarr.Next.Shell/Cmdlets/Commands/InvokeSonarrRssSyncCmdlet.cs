using MG.Sonarr.Next.Models.Commands;
using MG.Sonarr.Next.Services.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Commands
{
    [Cmdlet(VerbsLifecycle.Invoke, "SonarrRssSync", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public sealed class InvokeSonarrRssSyncCmdlet : SonarrCmdletBase
    {
        [Parameter]
        public CommandPriority Priority { get; set; }

        [Parameter]
        public SwitchParameter UpdateScheduledTask { get; set; }

        protected override void Process(IServiceProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            var tracker = provider.GetRequiredService<ICommandTracker>();

            var response = tracker.SendRssSync(this.Priority, this.UpdateScheduledTask.ToBool());
            if (response.IsError)
            {
                this.WriteError(response.Error);
            }
            else
            {
                this.WriteObject(response.Data);
            }
        }
    }
}
