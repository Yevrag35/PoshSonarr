using MG.Sonarr.Next.Shell.Context;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets
{
    public abstract class SonarrCmdletBase : Cmdlet
    {
        IServiceScope Services { get; }

        protected SonarrCmdletBase()
        {
            this.Services = this.CreateScope();
        }

        protected override void EndProcessing()
        {
            this.Services.Dispose();
        }
        protected override void StopProcessing()
        {
            this.Services.Dispose();
        }
    }
}
