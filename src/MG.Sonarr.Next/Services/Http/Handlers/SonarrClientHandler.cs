using MG.Sonarr.Next.Services.Auth;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace MG.Sonarr.Next.Services.Http.Handlers;

/// <summary>
/// Provides the configuration of the primary handler that sends a connection's requests, a
/// <see cref="SocketsHttpHandler"/> with the connection's proxy, cookies, and certificate settings.
/// </summary>
/// <remarks>
/// <para>Every primary handler of a connection shares the connection's one <see cref="CookieContainer"/>, so a cookie
/// that one handler received, such as a forms login's, goes out with the requests that the others send.</para>
/// <para>The handler sends through the proxy that <see cref="IConnectionSettings.TryGetProxy(out IWebProxy?)"/> gives,
/// or, when there's none, answers a server's authentication challenge with the user's default credentials. It accepts
/// any server certificate when <see cref="IConnectionSettings.SkipCertValidation"/> is <see langword="true"/>, and it
/// follows redirects.</para>
/// <para>The handler drains no response body. When a response is disposed before its body has been read to its end,
/// the handler closes the response's connection rather than reading the rest of the body to reuse the connection. A
/// canceled token or a timeout ends a synchronous read of a body by disposing the response, and the read ends only when
/// the connection closes, which a drain would put off by up to two seconds. The clients read every other response to
/// its end, so only a request that ends early gives up its connection.</para>
/// </remarks>
internal static class SonarrClientHandler
{
	/// <summary>
	/// Applies a connection's settings to the <see cref="SocketsHttpHandler"/> that sends its requests.
	/// </summary>
	/// <remarks>
	/// The method fits the delegate that <c>UseSocketsHttpHandler</c> takes, and the client factory calls it for each
	/// primary handler that it creates.
	/// </remarks>
	/// <param name="handler">The primary handler to configure.</param>
	/// <param name="provider">The provider of the connection's <see cref="IConnectionSettings"/> and the
	/// <see cref="CookieContainer"/> that its handlers share.</param>
	internal static void Configure(SocketsHttpHandler handler, IServiceProvider provider)
	{
		IConnectionSettings settings = provider.GetRequiredService<IConnectionSettings>();

		// One container serves every handler of the connection, so a forms login's cookie outlives the handler that got it.
		handler.CookieContainer = provider.GetRequiredService<CookieContainer>();

		// A body that's disposed before it's read to its end closes its connection at once, so a canceled read of a body
		// that the server has stopped sending ends without waiting out a drain.
		handler.MaxResponseDrainSize = 0;

		if (settings.TryGetProxy(out IWebProxy? proxy))
		{
			handler.UseProxy = true;
			handler.Proxy = proxy;
		}
		else
		{
			handler.Credentials = CredentialCache.DefaultCredentials;
		}

		if (settings.SkipCertValidation)
		{
			handler.SslOptions.RemoteCertificateValidationCallback = SkipValidation;
		}
	}

	/// <summary>
	/// Accepts a server certificate without validating it.
	/// </summary>
	/// <param name="sender">The object that validates the certificate.</param>
	/// <param name="certificate">The server's certificate.</param>
	/// <param name="chain">The certificate's chain.</param>
	/// <param name="errors">The errors that validating the certificate found.</param>
	/// <returns>Always <see langword="true"/>.</returns>
	private static bool SkipValidation(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors errors) => true;
}
