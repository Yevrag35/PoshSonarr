using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Services.Http.Requests;
using System.Net;

namespace MG.Sonarr.Next.Exceptions;

/// <summary>
/// Represents an exception thrown when an HTTP request to Sonarr fails.
/// </summary>
public sealed class SonarrHttpException : PoshSonarrException, IHttpRequestUri
{
	///// <summary>
	///// The stack trace, if any, received from the Sonarr server.
	///// </summary>
	//public string? Description { get; }
	/// <summary>
	/// The response body as extended information, if available.
	/// </summary>
	/// <value>The parsed error collection from the response body.</value>
	public IErrorCollection ExtendedInfo { get; }
	/// <summary>
	/// The headers from the <see cref="HttpResponseMessage"/> that generated this exception.
	/// </summary>
	/// <value>The response headers.</value>
	public IReadOnlyDictionary<string, string> Headers { get; }
	/// <summary>
	/// Indicates whether the response's status code was <see cref="HttpStatusCode.NotFound"/>.
	/// </summary>
	/// <value><see langword="true"/> when the status code is <see cref="HttpStatusCode.NotFound"/>; otherwise, <see langword="false"/>.</value>
	public bool IsNotFound => HttpStatusCode.NotFound == this.StatusCode;
	/// <summary>
	/// Gets the status code of the response that generated the exception.
	/// </summary>
	/// <value>The HTTP status code, or <see langword="null"/> when unavailable.</value>
	public HttpStatusCode? StatusCode { get; }
	/// <summary>
	/// The response message that generated the current exception.
	/// </summary>
	/// <value>The response message, or <see langword="null"/> when unavailable.</value>
	public HttpResponseMessage? Response { get; }
	/// <summary>
	/// Gets the reason phrase which is typically is sent by servers together with the status code
	/// from the response that generated the current exception.
	/// </summary>
	/// <value>The reason phrase, or <see langword="null"/> when unavailable.</value>
	public string? ReasonPhrase { get; }
	/// <summary>
	/// The <see cref="string"/> representation of the <see cref="Uri"/> of the request message or the
	/// original path provided that generated the current exception.
	/// </summary>
	/// <value>The request URI string, or <see langword="null"/> when unavailable.</value>
	public string? RequestUri { get; }

	/// <summary>
	/// Initializes a new <see cref="SonarrHttpException"/> instance.
	/// </summary>
	/// <param name="request">The failed request.</param>
	/// <param name="response">The HTTP response, or <see langword="null"/>.</param>
	[DebuggerStepThrough]
	public SonarrHttpException(HttpRequestMessage request, HttpResponseMessage? response)
		: this(request, response, ErrorCollection.Empty, null)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="SonarrHttpException"/> instance.
	/// </summary>
	/// <param name="request">The failed request.</param>
	/// <param name="response">The HTTP response, or <see langword="null"/>.</param>
	/// <param name="errors">The parsed server errors.</param>
	[DebuggerStepThrough]
	public SonarrHttpException(HttpRequestMessage request, HttpResponseMessage? response, IErrorCollection errors)
		: this(request, response, errors, null)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="SonarrHttpException"/> instance.
	/// </summary>
	/// <param name="request">The failed request.</param>
	/// <param name="response">The HTTP response, or <see langword="null"/>.</param>
	/// <param name="errors">The parsed server errors.</param>
	/// <param name="innerException">The exception that caused the current exception.</param>
	public SonarrHttpException(HttpRequestMessage request, HttpResponseMessage? response, IErrorCollection errors, Exception? innerException)
		: base(GetMessage(request, response, innerException, errors, out string? reqUri), innerException)
	{
		this.ExtendedInfo = errors;
		this.RequestUri = reqUri;
		this.Response = response;
		this.ReasonPhrase = response?.ReasonPhrase;
		this.StatusCode = response?.StatusCode;
		this.Headers = ParseResponseHeaders(response);
	}

	/// <summary>
	/// Builds the exception's message from the status of the response and the details of the failure.
	/// </summary>
	/// <remarks>
	/// <para>The details are the messages of the server's errors, one per line, or else the innermost exception's
	/// message, or else a general message.</para>
	/// <para>With a response, the message is the response's status text, such as <c>400 (Bad Request)</c>, then a colon
	/// and the details without their trailing whitespace, or the status text alone when the details are empty or only
	/// whitespace. Without one, the message is the details.</para>
	/// </remarks>
	/// <param name="request">The request that failed.</param>
	/// <param name="response">The response to the request, or <see langword="null"/> when there was none.</param>
	/// <param name="inner">The exception that caused the failure, or <see langword="null"/>.</param>
	/// <param name="errors">The errors that the server's response reported.</param>
	/// <param name="requestUri">
	/// When this method returns, the URL of <paramref name="request"/>, or <see langword="null"/> when it has none.
	/// </param>
	/// <returns>The exception's message.</returns>
	private static string GetMessage(HttpRequestMessage request, HttpResponseMessage? response, Exception? inner, IErrorCollection errors, out string? requestUri)
	{
		requestUri = GetRequestUri(request);
		string details = errors.Count <= 0
			? inner is null
				? "An exception occurred in response"
				: inner.GetBaseException().Message
			: errors.BuildMessage();

		if (response is null)
		{
			return details;
		}

		string status = response.StatusCode.AsResponseString;
		ReadOnlySpan<char> trimmed = details.AsSpan().TrimEnd();

		return trimmed.IsEmpty
			? status
			: $"{status}: {trimmed}";
	}

	/// <summary>
	/// Gets the request URI string from the specified request.
	/// </summary>
	/// <param name="request">The request to inspect.</param>
	/// <returns>The request URI string, or <see langword="null"/> when unavailable.</returns>
	private static string? GetRequestUri(HttpRequestMessage request)
	{
		return request.RequestUri?.ToString();
	}

	/// <summary>
	/// Parses HTTP response headers into a read-only dictionary.
	/// </summary>
	/// <param name="response">The response that provides headers.</param>
	/// <returns>A read-only header dictionary.</returns>
	private static IReadOnlyDictionary<string, string> ParseResponseHeaders(HttpResponseMessage? response)
	{
		if (response is null)
		{
			EmptyNameDictionary<string> empty = [];
			return empty;
		}

		Dictionary<string, string> dict = new(3);
		foreach (var header in response.Headers)
		{
			_ = dict.TryAdd(header.Key, string.Join(Environment.NewLine, header.Value));
		}

		return dict;
	}
}