using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Http
{
    public interface IApiCmdlet
    {
        void WriteVerbose(HttpRequestMessage request);
        void WriteVerboseSonarrResult(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options = null);
    }
}
