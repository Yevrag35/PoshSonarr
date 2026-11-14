using System.Security;
using MG.Sonarr.Next.Extensions.Pwsh;

namespace MG.Sonarr.Next.Tests.Extensions;

/// <summary>
/// Tests the conversion of a <see cref="PSCredential"/> into the credentials that a download logs in with.
/// </summary>
public sealed class PSCredentialExtensionsTests
{
	/// <summary>
	/// The credentials keep the credential's user name whole, where <see cref="PSCredential.GetNetworkCredential"/> splits
	/// a domain off at a backslash, or at an <c>@</c> with no dot after it, and a login would send only what's left. They
	/// hold the credential's password, and no domain.
	/// </summary>
	/// <param name="userName">The credential's user name.</param>
	[Theory]
	[InlineData(@"DOM\mike")]
	[InlineData("mike@home")]
	[InlineData("mike@example.com")]
	[InlineData("mike")]
	public void KeepsTheUserNameWhole(string userName)
	{
		using SecureString password = new();
		foreach (char c in "pässwörd")
		{
			password.AppendChar(c);
		}

		password.MakeReadOnly();
		PSCredential credential = new(userName, password);

		NetworkCredential? converted = credential.ToLoginCredential();

		Assert.NotNull(converted);
		Assert.Equal(userName, converted.UserName);
		Assert.Equal(string.Empty, converted.Domain);
		Assert.Equal("pässwörd", converted.Password);
	}

	/// <summary>
	/// No credential, and <see cref="PSCredential.Empty"/>, whose password is <see langword="null"/>, give no
	/// credentials, so a download sends none.
	/// </summary>
	[Fact]
	public void GivesNoCredentialsForNoCredential()
	{
		Assert.Null(((PSCredential?)null).ToLoginCredential());
		Assert.Null(PSCredential.Empty.ToLoginCredential());
	}
}
