using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Http.Extensions;
using MG.Sonarr.Next.Services.Http.Requests;

namespace MG.Sonarr.Next.Services.Http.Handlers;

public sealed class PathHandler : DelegatingHandler
{
	const string API = "/api";
	const string API_V3 = "api/v3";
	const string V3 = "/v3/";
	internal const string Ping = "IsPing";
	readonly string _basePath;
	readonly bool _noApiInPath;

	public PathHandler(IConnectionSettings settings)
	{
		_basePath = settings.ServiceUri.AbsolutePath.TrimEnd('/');
		_noApiInPath = settings.NoApiInPath;
	}

	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request is not AuthedRequestMessage)
		{
			this.SetPath(request);
		}

		return base.Send(request, cancellationToken);
	}

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request is not AuthedRequestMessage)
		{
			this.SetPath(request);
		}

		return base.SendAsync(request, cancellationToken);
	}

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

	private static bool IsApiPathUnderBase(ReadOnlySpan<char> path, ReadOnlySpan<char> basePath)
	{
		// A path that Sonarr hands out can hold the URL base already. The base starts with the slash that the path lost.
		ReadOnlySpan<char> baseSegments = basePath[1..];
		return path.StartsWith(baseSegments, StringComparison.OrdinalIgnoreCase)
			&& path[baseSegments.Length..].StartsWith("/api/v3", StringComparison.OrdinalIgnoreCase);
	}
}
