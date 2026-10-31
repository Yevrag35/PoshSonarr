using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Http.Handlers;
using MG.Sonarr.Next.Services.Http.Requests;
using Microsoft.Extensions.DependencyInjection;
using System.Management.Automation;
using System.Net;

namespace MG.Sonarr.Next.Services.Http.Clients;

/// <summary>
/// Provides methods that save a file that Sonarr serves, such as a backup or a log file, to a path.
/// </summary>
/// <remarks>
/// <para>
/// A relative download URL gets the connection's URL base in front of it, unless it starts with the base already, in
/// any case. An absolute URL goes out unchanged.
/// </para>
/// <para>
/// A response whose status isn't a success gives an error result, and nothing is written, since the body of such a
/// response isn't the file.
/// </para>
/// <para>
/// An instance takes the cmdlet of the DI scope that resolves it, so resolve it from a scope, not from the root provider.
/// </para>
/// </remarks>
public interface ISonarrDownloadClient
{
	/// <summary>
	/// Downloads the file at the specified URL to the specified path asynchronously.
	/// </summary>
	/// <remarks>
	/// A server whose authentication needs credentials fails the download, since this overload sends none.
	/// </remarks>
	/// <param name="url">The download URL that Sonarr gave, relative to the server or absolute.</param>
	/// <param name="path">The path of the file to write, which the method creates or overwrites.</param>
	/// <param name="token">A token that cancels the download.</param>
	/// <returns>
	/// A task that completes with a result that holds <paramref name="path"/> once the file is written, or an error when the
	/// download fails.
	/// </returns>
	Task<SonarrClientResult<string>> DownloadToPathAsync(string url, string path, CancellationToken token = default);
	/// <summary>
	/// Downloads the file at the specified URL to the specified path asynchronously, with the specified credentials for a
	/// server whose authentication needs them.
	/// </summary>
	/// <remarks>
	/// The credentials go out only when the server's authentication type, which the connection test stored, needs them:
	/// in an <c>Authorization: Basic</c> header for Basic authentication, or in a login to the server's login page for
	/// forms authentication.
	/// </remarks>
	/// <param name="url">The download URL that Sonarr gave, relative to the server or absolute.</param>
	/// <param name="path">The path of the file to write, which the method creates or overwrites.</param>
	/// <param name="credential">The credentials to authenticate with, or <see langword="null"/> to send none.</param>
	/// <param name="token">A token that cancels the download.</param>
	/// <returns>
	/// A task that completes with a result that holds <paramref name="path"/> once the file is written, or an error when the
	/// download fails.
	/// </returns>
	Task<SonarrClientResult<string>> DownloadToPathAsync(string url, string path, NetworkCredential? credential, CancellationToken token = default);
}

/// <summary>
/// Provides the <see cref="ISonarrDownloadClient"/> that downloads a connection's files through its typed
/// <see cref="HttpClient"/>.
/// </summary>
/// <remarks>
/// The client's handler chain has no <see cref="PathHandler"/>, so the client puts the URL base in front of a relative
/// URL itself.
/// </remarks>
internal sealed class SonarrDownloadClient : ISonarrDownloadClient
{
	/// <summary>
	/// The connection's URL base, with its leading slash and without its trailing one, or an empty string when the
	/// connection has none.
	/// </summary>
	readonly string _basePath;
	/// <summary>
	/// The typed client that sends the download requests.
	/// </summary>
	readonly HttpClient _client;
	/// <summary>
	/// The accessor that holds the cmdlet of the scope that resolved the client.
	/// </summary>
	readonly ApiCmdletAccessor _cmdletAccessor;
	/// <summary>
	/// The factory that each request message opens its scope from.
	/// </summary>
	readonly IServiceScopeFactory _scopeFactory;

	/// <summary>
	/// Initializes a new <see cref="SonarrDownloadClient"/> instance with the specified typed client and the connection's
	/// services.
	/// </summary>
	/// <param name="client">The typed client that sends the download requests.</param>
	/// <param name="settings">The connection's settings, whose service URI gives the URL base.</param>
	/// <param name="cmdletAccessor">The accessor of the resolving scope, which holds the scope's cmdlet.</param>
	/// <param name="scopeFactory">The factory that each request message opens its scope from.</param>
	public SonarrDownloadClient(HttpClient client, IConnectionSettings settings, ApiCmdletAccessor cmdletAccessor, IServiceScopeFactory scopeFactory)
	{
		_basePath = settings.ServiceUri.AbsolutePath.TrimEnd('/');
		_client = client;
		_cmdletAccessor = cmdletAccessor;
		_scopeFactory = scopeFactory;
	}

	/// <inheritdoc/>
	public async Task<SonarrClientResult<string>> DownloadToPathAsync(string url, string path, CancellationToken token = default)
	{
		using ApiKeyRequestMessage msg = new(HttpMethod.Get, this.AddUrlBase(url), _scopeFactory);

		return await this.DownloadAsync(msg, url, path, token).ConfigureAwait(false);
	}
	/// <inheritdoc/>
	public async Task<SonarrClientResult<string>> DownloadToPathAsync(string url, string path, NetworkCredential? credential, CancellationToken token = default)
	{
		using AuthedRequestMessage msg = new(HttpMethod.Get, this.AddUrlBase(url), credential, _scopeFactory);

		return await this.DownloadAsync(msg, url, path, token).ConfigureAwait(false);
	}

	/// <summary>
	/// Puts the URL base in front of the specified download URL, unless the URL starts with it already.
	/// </summary>
	/// <remarks>
	/// Sonarr gives a backup's path without the URL base, and a log file's download URL with it. The URL that this method
	/// returns is rooted, so combining it with the base address keeps only the address's scheme and server. An absolute
	/// URL comes back unchanged.
	/// </remarks>
	/// <param name="url">The download URL that Sonarr or the user gave.</param>
	/// <returns>The URL to request.</returns>
	private string AddUrlBase(string url)
	{
		if (!Uri.TryCreate(url, UriKind.Relative, out _))
		{
			return url;
		}

		ReadOnlySpan<char> path = url.AsSpan().TrimStart('/');

		return IsUnderBase(path, _basePath)
			? string.Concat("/", path)
			: string.Concat(_basePath, "/", path);
	}

	/// <summary>
	/// Determines whether the specified path starts with the segments of the URL base.
	/// </summary>
	/// <param name="path">The download path, without its leading slash.</param>
	/// <param name="basePath">The URL base, with its leading slash, or an empty span when the connection has none.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="basePath"/> isn't empty and <paramref name="path"/> starts with its
	/// segments; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool IsUnderBase(ReadOnlySpan<char> path, ReadOnlySpan<char> basePath)
	{
		if (basePath.IsEmpty)
		{
			return false;
		}

		// Sonarr v3 lowercases the URL base in the URLs that it gives, and the connection's URL has the case the user typed.
		ReadOnlySpan<char> baseSegments = basePath[1..];
		return path.StartsWith(baseSegments, StringComparison.OrdinalIgnoreCase)
			&& (path.Length == baseSegments.Length || path[baseSegments.Length] == '/');
	}

	//BUG: It catches only SonarrHttpException, so a refused connection, a timeout, a cancellation, or a path that can't be written throws out of the client instead of returning an error result.
	/// <summary>
	/// Sends the specified download request and writes the file that the response holds to a path.
	/// </summary>
	/// <param name="request">The request for the file.</param>
	/// <param name="url">The download URL that the caller gave, which an error reports when it has no other URL.</param>
	/// <param name="path">The path of the file to write.</param>
	/// <param name="token">The token that cancels the download.</param>
	/// <returns>
	/// A result that holds <paramref name="path"/> once the file is written, or an error when the request fails, the
	/// server answers with a status that isn't a success, or the response holds no file.
	/// </returns>
	private async Task<SonarrClientResult<string>> DownloadAsync(SonarrRequestMessage request, string url, string path, CancellationToken token)
	{
		_cmdletAccessor.AttachTo(request);

		HttpResponseMessage response;
		try
		{
			response = await _client.SendAsync(request, token).ConfigureAwait(false);
		}
		catch (SonarrHttpException ex)
		{
			return FromException(ex, ErrorHandler.NoResponseCode, response: null, url);
		}

		using (response)
		{
			if (!response.IsSuccessStatusCode)
			{
				// A download URL serves a file, so the body of an error response, often an HTML page, isn't the file.
				SonarrHttpException statusEx = new(request, response, ErrorCollection.Empty,
					new HttpRequestException("The Sonarr server didn't return the file.", null, response.StatusCode));

				return FromException(statusEx, response.StatusCode, response, url);
			}

			bool written = await WriteFileAsync(response, path, token).ConfigureAwait(false);

			return written
				? SonarrClientResult.Create(path, response, url)
				: SonarrClientResult.FromException<string>(new SonarrHttpException(request, response, ErrorCollection.Empty, null), ErrorCategory.WriteError, response.StatusCode, response);
		}
	}

	/// <summary>
	/// Creates the error result of a download that failed with the specified exception.
	/// </summary>
	/// <param name="exception">The exception that describes the failure.</param>
	/// <param name="statusCode">
	/// The status code of the response, or <see cref="ErrorHandler.NoResponseCode"/> when there's none.
	/// </param>
	/// <param name="response">The response to the download request, or <see langword="null"/> when there's none.</param>
	/// <param name="url">The download URL that the result reports when the exception gives no URL.</param>
	/// <returns>An error result for the download.</returns>
	private static SonarrClientResult<string> FromException(SonarrHttpException exception, HttpStatusCode statusCode, HttpResponseMessage? response, string url)
	{
		var result = SonarrClientResult.FromException<string>(exception, ErrorCategory.InvalidResult, statusCode, response);
		if (string.IsNullOrEmpty(result.RequestUrl))
		{
			result.RequestUrl = url;
		}

		return result;
	}

	/// <summary>
	/// Writes the body of the specified response to a file.
	/// </summary>
	/// <remarks>
	/// A body that is known to be empty writes nothing, since it holds no file. The method creates the file, or overwrites
	/// one that exists, and holds it open without sharing while it writes.
	/// </remarks>
	/// <param name="response">The successful response whose body is the file.</param>
	/// <param name="path">The path of the file to write.</param>
	/// <param name="token">A token that cancels reading the body and writing the file.</param>
	/// <returns>
	/// A task that completes with <see langword="true"/> if the file was written; otherwise, <see langword="false"/>, when
	/// the body is empty.
	/// </returns>
	private static async Task<bool> WriteFileAsync(HttpResponseMessage response, string path, CancellationToken token)
	{
		Stream stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
		await using (stream.ConfigureAwait(false))
		{
			if (stream.CanSeek && stream.Length <= 0)
			{
				return false;
			}

			FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);
			await using (fs.ConfigureAwait(false))
			{
				await stream.CopyToAsync(fs, token).ConfigureAwait(false);
				return true;
			}
		}
	}
}

/// <summary>
/// Provides the registration of the module's <see cref="ISonarrDownloadClient"/>.
/// </summary>
internal static class SonarrDownloadClientDependencyInjection
{
	/// <summary>
	/// Registers <see cref="SonarrDownloadClient"/> as the typed <see cref="ISonarrDownloadClient"/>, with
	/// <see cref="AuthHandler"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A request passes through <see cref="VerboseHandler"/>, <see cref="DebugSerializeHandler"/>, and
	/// <see cref="AuthHandler"/>, in that order, before <see cref="SonarrClientHandler"/> sends it. The typed client takes
	/// the connection's service URI as its base address and its timeout as its timeout, and sends the API key and the
	/// module's user agent on every request.
	/// </para>
	/// <para>
	/// The method registers neither <see cref="VerboseHandler"/>, <see cref="DebugSerializeHandler"/>, nor
	/// <see cref="SonarrClientHandler"/>, so the caller registers them.
	/// </para>
	/// </remarks>
	/// <param name="services">The service collection to add the registration to.</param>
	/// <returns><paramref name="services"/>, so that calls can be chained.</returns>
	internal static IServiceCollection AddSonarrDownloadClient(this IServiceCollection services)
	{
		services
			.AddTransient<AuthHandler>()
			.AddHttpClient<ISonarrDownloadClient, SonarrDownloadClient>((provider, client) =>
			{
				var settings = provider.GetRequiredService<IConnectionSettings>();
				client.BaseAddress = settings.ServiceUri;
				client.Timeout = settings.Timeout;
				client.DefaultRequestHeaders
					.Add(SonarrClientDependencyInjection.API_HEADER_KEY, settings.ApiKey.GetValue());

				client.DefaultRequestHeaders.UserAgent
					.Add(SonarrClientDependencyInjection.UserAgent);
			})
			.ConfigurePrimaryHttpMessageHandler<SonarrClientHandler>()
			.AddHttpMessageHandler<VerboseHandler>()
			.AddHttpMessageHandler<DebugSerializeHandler>()
			.AddHttpMessageHandler<AuthHandler>();

		return services;
	}
}