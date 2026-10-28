using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Http.Extensions;
using MG.Sonarr.Next.Services.Http.Requests;

namespace MG.Sonarr.Next.Services.Http.Handlers;

public sealed class PathHandler : DelegatingHandler
{
	const string API = "/api";
	const string V3 = "/v3/";
	internal const string Ping = "IsPing";
	readonly bool _noApiInPath;

	public PathHandler(IConnectionSettings settings)
	{
		_noApiInPath = settings.NoApiInPath;
	}

	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (ShouldSetPath(request))
		{
			this.SetPath(request);
		}

		return base.Send(request, cancellationToken);
	}

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (ShouldSetPath(request))
		{
			this.SetPath(request);
		}

		return base.SendAsync(request, cancellationToken);
	}

	private static bool ShouldSetPath(HttpRequestMessage request)
	{
		return request is not AuthedRequestMessage && !request.Options.ContainsMetadata(Ping);
	}

	private void SetPath(HttpRequestMessage request)
	{
		const int max_stackalloc = 256;

		ReadOnlySpan<char> path = request.RequestUri?.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped);
		if (path.StartsWith('/'))
		{
			path = path.TrimStart('/');
		}

		if (path.StartsWith("api/v3", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		ReadOnlySpan<char> authority = request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);

		int length = path.Length + API.Length + V3.Length + authority.Length;
		using var builder = SpanStringBuilder.Create(
			length <= max_stackalloc
				? stackalloc char[length]
				: length);

		builder.Append(authority);

		if (!_noApiInPath)
		{
			builder.Append(API);
		}

		builder.Append(V3);
		if (path.StartsWith('/', StringComparison.Ordinal))
		{
			builder.Backspace();
		}

		builder.Append(path);

		request.RequestUri = new Uri(builder.ToString(), UriKind.Absolute);
	}
}
