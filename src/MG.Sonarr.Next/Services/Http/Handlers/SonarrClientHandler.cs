using MG.Sonarr.Next.Services.Auth;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace MG.Sonarr.Next.Services.Http.Handlers;

/// <summary>
/// Provides the primary handler that sends a connection's requests, with its proxy, cookies, and certificate
/// settings.
/// </summary>
/// <remarks>
/// <para>Every <see cref="SonarrClientHandler"/> of a connection shares the connection's one
/// <see cref="CookieContainer"/>, so a cookie that one handler received, such as a forms login's, goes out with the
/// requests that the others send.</para>
/// <para>The handler sends through the proxy that <see cref="IConnectionSettings.TryGetProxy(out IWebProxy?)"/> gives,
/// or, when there's none, answers a server's authentication challenge with the user's default credentials. It accepts
/// any server certificate when <see cref="IConnectionSettings.SkipCertValidation"/> is <see langword="true"/>, and it
/// follows redirects.</para>
/// </remarks>
internal sealed class SonarrClientHandler : HttpClientHandler
{
	/// <summary>
	/// Initializes a new <see cref="SonarrClientHandler"/> instance with the specified connection settings and cookie
	/// container.
	/// </summary>
	/// <param name="settings">The connection settings that supply the proxy and whether to skip certificate
	/// validation.</param>
	/// <param name="cookies">The cookie container that every primary handler of the connection shares.</param>
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

	/// <summary>
	/// Accepts a server certificate without validating it.
	/// </summary>
	/// <param name="msg">The request whose server sent the certificate.</param>
	/// <param name="cert">The server's certificate.</param>
	/// <param name="chain">The certificate's chain.</param>
	/// <param name="errors">The errors that validating the certificate found.</param>
	/// <returns>Always <see langword="true"/>.</returns>
	static bool SkipValidation(HttpRequestMessage msg, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors errors) => true;
}
