using MG.Sonarr.Next.Services.Auth;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace MG.Sonarr.Next.Services.Http.Handlers
{
    internal sealed class SonarrClientHandler : HttpClientHandler
    {
        public SonarrClientHandler(IConnectionSettings settings)
        {
            if (settings.TryGetProxy(out IWebProxy? proxy))
            {
                this.UseProxy = true;
                this.Proxy = proxy;
            }
            else
            {
                this.UseDefaultCredentials = true;
            }

            if (settings.SkipCertValidation)
            {
                this.ServerCertificateCustomValidationCallback = SkipValidation;
            }
        }

        static bool SkipValidation(HttpRequestMessage msg, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors errors) => true;
    }
}
