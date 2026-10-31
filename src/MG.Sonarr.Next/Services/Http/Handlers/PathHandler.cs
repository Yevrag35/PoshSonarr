using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Http.Extensions;
using MG.Sonarr.Next.Services.Http.Requests;

namespace MG.Sonarr.Next.Services.Http.Handlers;

/// <summary>
/// Provides a delegating handler that builds each request's URL from the connection's URL base, the API prefix, and the
/// endpoint path.
/// </summary>
/// <remarks>
/// <para>Callers pass endpoint paths that are relative to the API and rooted, such as <c>/series</c>, so the URL that
/// reaches the handler keeps only the scheme and server of the base address. The handler puts the URL base from
/// <see cref="IConnectionSettings.ServiceUri"/> after them, then the <c>/api/v3/</c> prefix, or <c>/v3/</c> when
/// <see cref="IConnectionSettings.NoApiInPath"/> is <see langword="true"/>, and then the path. The path and query keep
/// the escaping that the caller gave them.</para>
/// <para>A path that already starts with <c>api/v3</c>, and a ping request, get the URL base without the prefix. The
/// handler leaves alone a path that already starts with the URL base and <c>api/v3</c>, and a download request that
/// carries credentials.</para>
/// <para>The handler reads the connection settings once, in its constructor, and keeps no per-request state, so one
/// instance can serve concurrent requests.</para>
/// </remarks>
public sealed class PathHandler : DelegatingHandler
{
	const string API = "/api";
	const string API_V3 = "api/v3";
	const string V3 = "/v3/";
	/// <summary>
	/// The key of the request option that marks a ping request, whose path gets the URL base but no API prefix.
	/// </summary>
	internal const string Ping = "IsPing";
	readonly string _basePath;
	readonly bool _noApiInPath;

	/// <summary>
	/// Initializes a new <see cref="PathHandler"/> instance with the URL base and the API prefix setting of the specified
	/// connection settings.
	/// </summary>
	/// <param name="settings">The connection settings that supply the URL base and whether the path leaves out
	/// <c>/api</c>.</param>
	public PathHandler(IConnectionSettings settings)
	{
		_basePath = settings.ServiceUri.AbsolutePath.TrimEnd('/');
		_noApiInPath = settings.NoApiInPath;
	}

	/// <summary>
	/// Builds the request's URL and sends the request to the inner handler synchronously.
	/// </summary>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>The HTTP response message from the inner handler.</returns>
	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request is not AuthedRequestMessage)
		{
			this.SetPath(request);
		}

		return base.Send(request, cancellationToken);
	}

	/// <summary>
	/// Builds the request's URL and sends the request to the inner handler asynchronously.
	/// </summary>
	/// <param name="request">The HTTP request message to send.</param>
	/// <param name="cancellationToken">A token that cancels the operation.</param>
	/// <returns>A task that completes with the HTTP response message from the inner handler.</returns>
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request is not AuthedRequestMessage)
		{
			this.SetPath(request);
		}

		return base.SendAsync(request, cancellationToken);
	}

	/// <summary>
	/// Replaces the request's URL with its scheme and server, the URL base, the API prefix when the path needs one, and
	/// its path and query.
	/// </summary>
	/// <remarks>
	/// <para>The URL stays as it is when the path already starts with the URL base and <c>api/v3</c>, and when there's
	/// no URL base and the path either starts with <c>api/v3</c> or belongs to a request marked with
	/// <see cref="Ping"/>.</para>
	/// <para><b>Performance:</b> The new URL is built in a stack buffer when it's at most 256 characters long, and in a
	/// pooled buffer otherwise, so the only allocations are the URL's string and its <see cref="Uri"/>.</para>
	/// </remarks>
	/// <param name="request">The HTTP request message whose URL is replaced.</param>
	private void SetPath(HttpRequestMessage request)
	{
		const int max_stackalloc = 256;

		// The endpoint paths are rooted, so combining one with the base address keeps only the address's scheme and
		// server, and the URL base comes from the settings instead.
		ReadOnlySpan<char> path = request.RequestUri?.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped);
		if (path.StartsWith('/'))
		{
			path = path.TrimStart('/');
		}

		ReadOnlySpan<char> basePath = _basePath;
		if (!basePath.IsEmpty && IsApiPathUnderBase(path, basePath))
		{
			return;
		}

		bool addApi = !path.StartsWith(API_V3, StringComparison.OrdinalIgnoreCase)
			&& !request.Options.ContainsMetadata(Ping);

		if (!addApi && basePath.IsEmpty)
		{
			return;
		}

		ReadOnlySpan<char> authority = request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);

		int length = path.Length + API.Length + V3.Length + authority.Length + basePath.Length;
		using var builder = SpanStringBuilder.Create(
			length <= max_stackalloc
				? stackalloc char[length]
				: length);

		builder.Append(authority);
		builder.Append(basePath);

		if (!addApi)
		{
			builder.Append('/');
		}
		else
		{
			if (!_noApiInPath)
			{
				builder.Append(API);
			}

			builder.Append(V3);
		}

		builder.Append(path);

		request.RequestUri = new Uri(builder.ToString(), UriKind.Absolute);
	}

	/// <summary>
	/// Determines whether a path already starts with the URL base followed by <c>/api/v3</c>, as a path that Sonarr hands
	/// out can.
	/// </summary>
	/// <remarks>
	/// The comparison is ordinal and ignores case.
	/// </remarks>
	/// <param name="path">The request's path and query, without their leading slash.</param>
	/// <param name="basePath">The URL base, which starts with a slash and isn't empty.</param>
	/// <returns><see langword="true"/> if <paramref name="path"/> starts with the URL base and <c>/api/v3</c>;
	/// otherwise, <see langword="false"/>.</returns>
	private static bool IsApiPathUnderBase(ReadOnlySpan<char> path, ReadOnlySpan<char> basePath)
	{
		// A path that Sonarr hands out can hold the URL base already. The base starts with the slash that the path lost.
		ReadOnlySpan<char> baseSegments = basePath[1..];
		return path.StartsWith(baseSegments, StringComparison.OrdinalIgnoreCase)
			&& path[baseSegments.Length..].StartsWith("/api/v3", StringComparison.OrdinalIgnoreCase);
	}
}
