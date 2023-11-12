using MG.Sonarr.Next.Models.Commands;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Jobs;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.Commands
{
    [Cmdlet(VerbsLifecycle.Invoke, "SonarrRssSync", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public sealed class InvokeSonarrRssSyncCmdlet : TimedCmdlet
    {
        [Parameter]
        public CommandPriority Priority { get; set; }

        [Parameter]
        public SwitchParameter UpdateScheduledTask { get; set; }

        protected override void Process(IServiceProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            var tracker = provider.GetRequiredService<ICommandTracker>();
            var queue = provider.GetRequiredService<Queue<IApiCmdlet>>();
            queue.Enqueue(this);

            this.StartTimer();
            var response = tracker.SendRssSync(this.Priority, this.UpdateScheduledTask.ToBool());
            bool written = this.TryWriteObject(in response);
        }
    }
}
