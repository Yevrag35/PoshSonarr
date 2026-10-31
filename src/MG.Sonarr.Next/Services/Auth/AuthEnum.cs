namespace MG.Sonarr.Next.Services.Auth;

/// <summary>
/// Represents the authentication scheme used by a Sonarr server.
/// </summary>
public enum SonarrAuthType
{
	/// <summary>
	/// No authentication is required.
	/// </summary>
	None,
	/// <summary>
	/// HTTP basic authentication is required.
	/// </summary>
	Basic,
	/// <summary>
	/// Form-based authentication is required.
	/// </summary>
	Forms,
	/// <summary>
	/// External identity provider authentication is required.
	/// </summary>
	External,
}
