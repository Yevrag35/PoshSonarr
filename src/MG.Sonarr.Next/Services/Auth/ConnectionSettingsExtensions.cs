namespace MG.Sonarr.Next.Services.Auth;

/// <summary>
/// Provides the URL base that a connection's downloads and forms login go to.
/// </summary>
internal static class ConnectionSettingsExtensions
{
	/// <summary>
	/// Provides the URL base of a connection's downloads and forms login.
	/// </summary>
	/// <param name="settings">The connection's settings.</param>
	extension(IConnectionSettings settings)
	{
		/// <summary>
		/// Gets the URL base that a download and a forms login go to, spelled as the server spells it.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The URL base is the path of <see cref="IConnectionSettings.ServiceUri"/> without its trailing slash. When
		/// <see cref="IConnectionSettings.ServerUrlBase"/> matches it without regard to case, the server's spelling takes
		/// its place, so a download URL that Sonarr built, a login, and the cookie that the login sets all spell the URL
		/// base the same way, and the cookie goes back with every download.
		/// </para>
		/// <para>
		/// A server's URL base that differs by more than case, as behind a reverse proxy that maps one path to another,
		/// leaves the connection's in place, and so does a server that reported none.
		/// </para>
		/// </remarks>
		/// <returns>
		/// The URL base with its leading slash and without a trailing one, such as <c>/sonarr</c>, or an empty string for
		/// a connection without one.
		/// </returns>
		internal string GetUrlBase()
		{
			string urlBase = settings.ServiceUri.AbsolutePath.TrimEnd('/');
			string? serverUrlBase = settings.ServerUrlBase?.TrimEnd('/');

			return serverUrlBase is not null && urlBase.Equals(serverUrlBase, StringComparison.OrdinalIgnoreCase)
				? serverUrlBase
				: urlBase;
		}
	}
}
