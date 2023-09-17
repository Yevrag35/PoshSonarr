using MG.Sonarr.Next.Services.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Services.Http
{
    internal sealed class SonarrClientHandler : HttpClientHandler
    {
        public SonarrClientHandler(IConnectionSettings settings)
            : base()
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
