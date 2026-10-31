using System.Net;

namespace MG.Sonarr.Next.Services.Http;

/// <summary>
/// Represents the outcome of a request to a Sonarr API endpoint.
/// </summary>
public interface ISonarrResponse
{
	/// <summary>
	/// Gets the error record of the response.
	/// </summary>
	/// <value>
	/// The record that describes the failure, or <see langword="null"/> when <see cref="IsError"/> is
	/// <see langword="false"/>.
	/// </value>
	SonarrErrorRecord? Error { get; }

	/// <summary>
	/// Gets a value that indicates whether the request failed.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the request failed and <see cref="Error"/> describes why; otherwise,
	/// <see langword="false"/>.
	/// </value>
	[MemberNotNullWhen(true, nameof(Error))]
	bool IsError { get; }
	/// <summary>
	/// Gets the HTTP status code of the response.
	/// </summary>
	/// <value>The status code that the server returned, or a code above 599 when no response was received.</value>
	HttpStatusCode StatusCode { get; }

	/// <summary>
	/// Gets the URL that the response's request was sent to.
	/// </summary>
	/// <value>The request's URL as a string, or an empty string when it isn't known.</value>
	string RequestUrl { get; }
}

/// <summary>
/// Represents the outcome of a timed request to a Sonarr API endpoint, with the time it took and the services of the
/// request's scope.
/// </summary>
public interface ISonarrTimedResponse : ISonarrResponse
{
	/// <summary>
	/// Gets the time between when the request was sent and when the response was received.
	/// </summary>
	/// <value>The elapsed time of the request.</value>
	TimeSpan Elapsed { get; }
	/// <summary>
	/// Gets a scoped service provider for code that handles the response.
	/// </summary>
	/// <value>The service provider of the request's scope.</value>
	IServiceProvider Services { get; }
}
