using MG.Sonarr.Next.Services.Auth;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace MG.Sonarr.Next.Services.Http.Handlers;

internal sealed class SonarrClientHandler : HttpClientHandler
{
	public SonarrClientHandler(IConnectionSettings settings, CookieContainer cookies)
	{
		// One container serves every handler of the connection, so a forms login's cookie outlives the handler that got it.
		this.CookieContainer = cookies;

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
