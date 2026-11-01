using MG.Sonarr.Next.Services.Auth;

namespace MG.Sonarr.Next.Models;

/// <summary>
/// Represents the part of Sonarr's system status that the connection test reads.
/// </summary>
public sealed class SonarrStatus
{
	/// <summary>
	/// Gets or sets the authentication method that the server reports.
	/// </summary>
	/// <value>The name of the server's authentication method, such as <c>forms</c>, or <see langword="null"/>.</value>
	public string? Authentication { get; set; }

	/// <summary>
	/// Parses <see cref="Authentication"/> as a <see cref="SonarrAuthType"/>, without regard to case.
	/// </summary>
	/// <param name="authType">
	/// When this method returns, the parsed authentication type if parsing succeeded; otherwise, the default value.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <see cref="Authentication"/> parsed as a <see cref="SonarrAuthType"/>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public bool TryGetAuthType(out SonarrAuthType authType)
	{
		return Enum.TryParse(this.Authentication, ignoreCase: true, out authType);
	}
}
