namespace MG.Sonarr.Next.Services.Http;

/// <summary>
/// Represents one HTTP exchange with Sonarr: the request that was sent, the path it was sent to, and the response, if any.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="IResponseReader"/> receives an instance to turn the exchange into a <see cref="SonarrClientResult"/>.
/// </para>
/// <para>
/// The instance doesn't own <see cref="Request"/> or <see cref="Response"/>, and it disposes neither. The code that sent the
/// request disposes both after the reader returns.
/// </para>
/// </remarks>
public sealed class HttpCall
{
	/// <summary>
	/// Gets a value that indicates whether the server returned a response.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if <see cref="Response"/> isn't <see langword="null"/>; otherwise, <see langword="false"/>.
	/// </value>
	[MemberNotNullWhen(true, nameof(Response))]
	public bool HasResponse { get; }
	/// <summary>
	/// Gets the HTTP method of the request.
	/// </summary>
	/// <value>The method of <see cref="Request"/>.</value>
	public HttpMethod Method { get; }
	/// <summary>
	/// Gets the request that was sent.
	/// </summary>
	/// <value>The request message.</value>
	public HttpRequestMessage Request { get; }
	/// <summary>
	/// Gets the URL that the request was sent to.
	/// </summary>
	/// <value>
	/// The request's URI as a string, or the path that the instance was created with when the request has no URI.
	/// </value>
	public string RequestUri { get; }
	/// <summary>
	/// Gets the response that the server returned.
	/// </summary>
	/// <value>The response message, or <see langword="null"/> when no response was received.</value>
	public HttpResponseMessage? Response { get; }

	/// <summary>
	/// Initializes a new <see cref="HttpCall"/> instance with the specified path, request, and response.
	/// </summary>
	/// <param name="path">
	/// The path that the request was sent to, which <see cref="RequestUri"/> falls back to when <paramref name="request"/>
	/// has no URI. This value must not be <see langword="null"/>, empty, or whitespace.
	/// </param>
	/// <param name="request">The request that was sent. This value must not be <see langword="null"/>.</param>
	/// <param name="response">The response that the server returned, or <see langword="null"/> when there was none.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> or <paramref name="request"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or consists only of whitespace.</exception>
	public HttpCall(string path, HttpRequestMessage request, HttpResponseMessage? response)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		ArgumentNullException.ThrowIfNull(request);

		this.Method = request.Method;
		this.Request = request;
		this.RequestUri = request.RequestUri?.ToString() ?? path;
		this.HasResponse = response is not null;
		this.Response = response;
	}
}
