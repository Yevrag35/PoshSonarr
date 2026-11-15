using System.Security;

namespace MG.Sonarr.Next.Extensions.Pwsh;

/// <summary>
/// Provides the conversion of a <see cref="PSCredential"/> into the credentials that a download logs in with.
/// </summary>
internal static class PSCredentialExtensions
{
	/// <summary>
	/// Provides the conversion of a credential into the credentials that a download logs in with.
	/// </summary>
	/// <param name="credential">The credential to convert, or <see langword="null"/>.</param>
	extension(PSCredential? credential)
	{
		/// <summary>
		/// Creates the credentials that a download logs in with, from the credential's user name and password.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The user name goes into the new credentials as it is. <see cref="PSCredential.GetNetworkCredential"/> would
		/// split one that holds a backslash, or an <c>@</c> with no dot after it, into a user name and a domain, such as
		/// <c>mike</c> and <c>DOM</c> for <c>DOM\mike</c>, and a login sends only the user name.
		/// </para>
		/// <para>
		/// The method reads the password without copying it, and the new credentials keep a copy of their own. It doesn't
		/// check that the user name and the password aren't blank; a cmdlet's validation of its argument does.
		/// </para>
		/// </remarks>
		/// <returns>
		/// The new credentials, or <see langword="null"/> when the credential is <see langword="null"/> or
		/// <see cref="PSCredential.Empty"/>, whose password is <see langword="null"/>.
		/// </returns>
		internal NetworkCredential? ToLoginCredential()
		{
			return credential?.Password is SecureString password
				? new NetworkCredential(credential.UserName, password)
				: null;
		}
	}
}
