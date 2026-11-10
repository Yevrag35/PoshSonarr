using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;

namespace MG.Sonarr.Next.Tests.Http;

/// <summary>
/// Calls each send method of <see cref="ISonarrClient"/> as a cmdlet does, blocking on an asynchronous one with
/// <c>GetAwaiter().GetResult()</c>, which a test method mustn't do itself.
/// </summary>
internal static class ClientCalls
{
	/// <summary>
	/// The endpoint that every call sends to.
	/// </summary>
	internal const string PATH = "/tag";

	/// <summary>
	/// The URL of a file to download, as Sonarr gives a backup's path.
	/// </summary>
	internal const string DOWNLOAD_PATH = "/backup/manual/sonarr_backup.zip";

	/// <summary>
	/// The URL that a request to <see cref="PATH"/> goes to once the module's handlers have built it.
	/// </summary>
	internal static readonly string AbsoluteUrl = new Uri(FakeSonarr.BaseUri, "/api/v3" + PATH).AbsoluteUri;

	private static readonly object s_body = new { label = "body" };

	/// <summary>
	/// The methods that send an API request, by name.
	/// </summary>
	internal static readonly IReadOnlyDictionary<string, Func<ISonarrClient, CancellationToken, SonarrClientResult>> ApiMethods =
		new Dictionary<string, Func<ISonarrClient, CancellationToken, SonarrClientResult>>(StringComparer.Ordinal)
		{
			["SendDelete"] = (client, token) => client.SendDelete(PATH, token),
			["SendDeleteAsync"] = (client, token) => client.SendDeleteAsync(PATH, token).GetAwaiter().GetResult(),
			["SendGet"] = (client, token) => client.SendGet<object>(PATH, token),
			["SendGetAsync"] = (client, token) => client.SendGetAsync<object>(PATH, token).GetAwaiter().GetResult(),
			["SendPost"] = (client, token) => client.SendPost(PATH, s_body, token),
			["SendPost<T>"] = (client, token) => client.SendPost<object>(PATH, s_body, token),
			["SendPostAsync"] = (client, token) => client.SendPostAsync(PATH, s_body, token).GetAwaiter().GetResult(),
			["SendPostAsync<T>"] = (client, token) => client.SendPostAsync<object>(PATH, s_body, token).GetAwaiter().GetResult(),
			["SendPut"] = (client, token) => client.SendPut(PATH, s_body, token),
			["SendPutAsync"] = (client, token) => client.SendPutAsync(PATH, s_body, token).GetAwaiter().GetResult(),
		};

	/// <summary>
	/// Every send method, by name: the API methods, the ping, and the connection test.
	/// </summary>
	internal static readonly IReadOnlyDictionary<string, Func<ISonarrClient, CancellationToken, SonarrClientResult>> AllMethods =
		new Dictionary<string, Func<ISonarrClient, CancellationToken, SonarrClientResult>>(ApiMethods, StringComparer.Ordinal)
		{
			["SendPingAsync"] = (client, token) => client.SendPingAsync(token).GetAwaiter().GetResult(),
			["SendTest"] = (client, token) => client.SendTest(token),
		};
}
