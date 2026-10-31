namespace MG.Sonarr.Next.Exceptions;

/// <summary>
/// Represents an exception thrown when a successful HTTP response returns no expected content.
/// </summary>
public sealed class EmptyHttpResponseException : PoshSonarrException, Services.Http.Requests.IHttpRequestUri
{
	const string MSG = "An empty response was received when there should have been one.";

	/// <summary>
	/// Gets the URL of the request that generated the exception, if available.
	/// </summary>
	/// <value>The request URL, or <see langword="null"/> when unavailable.</value>
	public string? Url { get; }

	/// <inheritdoc/>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	string? Services.Http.Requests.IHttpRequestUri.RequestUri => this.Url;

	/// <summary>
	/// Initializes a new instance of the <see cref="EmptyHttpResponseException"/> class with the
	/// specified URL that the exception occurred from.
	/// </summary>
	/// <param name="url">The request URL.</param>
	public EmptyHttpResponseException(string? url) : base(MSG)
	{
		this.Url = url;
	}
}
