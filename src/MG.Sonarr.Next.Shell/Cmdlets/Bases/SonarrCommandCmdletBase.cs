using MG.Sonarr.Next.Services.Http;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets.Bases
{
    public abstract class SonarrCommandCmdletBase : SonarrCmdletBase, IApiCmdlet
    {
        public void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public void WriteVerboseBefore(IHttpRequestDetails request)
        {
            throw new NotImplementedException();
        }
    }
}
