using System.Security;

namespace MG.Sonarr.Next.Shell.Attributes;

/// <summary>
/// Provides the validation of a credential argument as one that a login can use: one with a user name and a password.
/// </summary>
/// <remarks>
/// <para>
/// Sonarr turns away a login with a blank user name or an empty password, so the attribute turns away such a credential
/// when the arguments bind, before the cmdlet sends anything. It reads the credential's own password, which it doesn't
/// copy.
/// </para>
/// <para>
/// It lets <see langword="null"/> and <see cref="PSCredential.Empty"/> through, since each means that the cmdlet sends no
/// credentials.
/// </para>
/// </remarks>
public sealed class ValidateLoginCredentialAttribute : ValidateArgumentsAttribute
{
	/// <summary>
	/// Checks that the argument has a user name and a password, unless it stands for no credentials.
	/// </summary>
	/// <param name="arguments">The argument, after the binder converted it to the parameter's type.</param>
	/// <param name="engineIntrinsics">The engine APIs of the session that binds the argument, which the method doesn't
	/// read.</param>
	/// <exception cref="ValidationMetadataException">Thrown when the argument's user name is white space or its password
	/// is empty.</exception>
	protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
	{
		if (arguments is PSCredential { Password: SecureString password } credential
			&& (string.IsNullOrWhiteSpace(credential.UserName) || password.Length == 0))
		{
			throw new ValidationMetadataException("The credential needs a user name and a password.");
		}
	}
}
