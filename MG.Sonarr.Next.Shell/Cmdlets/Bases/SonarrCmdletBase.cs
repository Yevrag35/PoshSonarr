using MG.Sonarr.Next.Shell.Context;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets
{
    public abstract class SonarrCmdletBase : PSCmdlet
    {
        IServiceScope Scope { get; }
        protected IServiceProvider Services => this.Scope.ServiceProvider;

        protected SonarrCmdletBase()
        {
            this.Scope = this.CreateScope();
        }

        protected override void EndProcessing()
        {
            this.Scope.Dispose();
        }
        protected override void StopProcessing()
        {
            this.Scope.Dispose();
        }
    }
}
