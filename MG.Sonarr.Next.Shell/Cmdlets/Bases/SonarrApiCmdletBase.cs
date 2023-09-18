using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Json;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets
{
    public abstract class SonarrApiCmdletBase : SonarrCmdletBase
    {
        ISonarrClient Client { get; }
        protected SonarrJsonOptions? Options { get; }

        protected SonarrApiCmdletBase()
            : base()
        {
            this.Client = this.Services.GetRequiredService<ISonarrClient>();
            this.Options = this.Services.GetService<SonarrJsonOptions>();
        }

        protected virtual SonarrResponse<T> SendGetRequest<T>(string path, CancellationToken token = default)
        {
            SonarrResponse<T> response = this.Client.SendGetAsync<T>(path, token).GetAwaiter().GetResult();
            this.WriteVerboseSonarrResult(response, this.Options?.GetForSerializing());

            return response;
        }

        protected virtual SonarrResponse SendPutRequest(string path, PSObject body , CancellationToken token = default)
        {
            SonarrResponse response = this.Client.SendPutAsync(path, body, token).GetAwaiter().GetResult();
            this.WriteVerboseSonarrResult(response, this.Options?.GetForSerializing());
            return response;
        }
    }
}