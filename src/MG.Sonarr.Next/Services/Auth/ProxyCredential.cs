using System.Management.Automation;
using System.Net;

namespace MG.Sonarr.Next.Services.Auth;

/// <summary>
/// Represents credentials that adapt PowerShell and network credentials for proxy authentication.
/// </summary>
/// <remarks>
/// This type wraps an <see cref="ICredentials"/> instance and forwards proxy credential resolution through
/// <see cref="ICredentials.GetCredential(Uri, string)"/>.
/// </remarks>
public sealed class ProxyCredential : ICredentials
{
	readonly ICredentials _nc;

	/// <summary>
	/// Initializes a new <see cref="ProxyCredential"/> instance with the specified credentials.
	/// </summary>
	/// <param name="icreds">The credentials used for proxy authentication.</param>
	public ProxyCredential(ICredentials icreds) => _nc = icreds;

	/// <summary>
	/// Initializes a new <see cref="ProxyCredential"/> instance with the specified PowerShell credential.
	/// </summary>
	/// <param name="psCreds">The PowerShell credential to convert into network credentials.</param>
	public ProxyCredential(PSCredential psCreds)
		: this(psCreds.GetNetworkCredential()) { }

	/// <summary>
	/// Converts a <see cref="PSCredential"/> value to a <see cref="ProxyCredential"/> instance.
	/// </summary>
	/// <param name="psCreds">The PowerShell credential to convert.</param>
	/// <returns>A proxy credential wrapper for the specified PowerShell credential.</returns>
	public static implicit operator ProxyCredential(PSCredential psCreds) => new(psCreds);

	/// <summary>
	/// Converts a <see cref="NetworkCredential"/> value to a <see cref="ProxyCredential"/> instance.
	/// </summary>
	/// <param name="icreds">The network credential to convert.</param>
	/// <returns>A proxy credential wrapper for the specified network credential.</returns>
	public static implicit operator ProxyCredential(NetworkCredential icreds) => new(icreds);

	/// <summary>
	/// Returns the credential used for the specified URI and authentication type.
	/// </summary>
	/// <param name="uri">The URI that requires credentials.</param>
	/// <param name="authType">The authentication scheme used to challenge the client.</param>
	/// <returns>The matching network credential, or <see langword="null"/> when no credential is available.</returns>
	NetworkCredential? ICredentials.GetCredential(Uri uri, string authType) => _nc.GetCredential(uri, authType);
}
