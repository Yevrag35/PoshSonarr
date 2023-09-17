using MG.Sonarr.Next.Services.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets
{
    public abstract class SonarrApiCmdletBase : SonarrCmdletBase
    {
        protected ISonarrClient Client { get; }

        protected SonarrApiCmdletBase()
            : base()
        {
            this.Client = this.Services.GetRequiredService<ISonarrClient>();
        }


    }
}