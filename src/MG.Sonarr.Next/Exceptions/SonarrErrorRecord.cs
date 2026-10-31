using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Services.Http.Requests;
using System.Management.Automation;
using System.Net;

namespace MG.Sonarr.Next.Exceptions;

/// <summary>
/// Represents an error thrown from the PoshSonarr PowerShell module.
/// </summary>
/// <inheritdoc cref="ErrorRecord"/>
public sealed class SonarrErrorRecord : ErrorRecord, IHttpRequestUri
{
	readonly IReadOnlyDictionary<string, string> _headers;

	/// <inheritdoc cref="SonarrHttpException.Headers"/>
	/// <value>The HTTP headers associated with the error.</value>
	public IReadOnlyDictionary<string, string> Headers => _headers;

	/// <inheritdoc cref="ErrorDetails.Message"/>
	/// <value>The error message text.</value>
	public string Message => this.ErrorDetails?.Message ?? this.Exception.Message;
	/// <summary>
	/// Indicates whether this error record can be written through <see cref="Cmdlet.WriteError(ErrorRecord)"/> conditionally. If <see langword="true"/>, this record should only be optionally
	/// written to the error stream.
	/// </summary>
	/// <value><see langword="true"/> when the error can be ignored; otherwise, <see langword="false"/>.</value>
	public bool IsIgnorable { get; }

	/// <inheritdoc cref="SonarrHttpException.StatusCode"/>
	/// <value>The HTTP status code when available.</value>
	public HttpStatusCode? StatusCode { get; }

	/// <inheritdoc cref="SonarrHttpException.ReasonPhrase"/>
	/// <value>The HTTP reason phrase when available.</value>
	public string? ReasonPhrase { get; }

	/// <inheritdoc cref="SonarrHttpException.RequestUri"/>
	/// <value>The request URI string when available.</value>
	public string? RequestUri { get; internal set; }

	/// <summary>
	/// Initializes a new <see cref="SonarrErrorRecord"/> instance from a Sonarr HTTP exception.
	/// </summary>
	/// <param name="exception">The Sonarr HTTP exception.</param>
	/// <param name="response">The HTTP response associated with the exception.</param>
	public SonarrErrorRecord(SonarrHttpException exception, HttpResponseMessage? response)
		: this(exception, response, (object?)null)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="SonarrErrorRecord"/> instance from a Sonarr HTTP exception.
	/// </summary>
	/// <param name="exception">The Sonarr HTTP exception.</param>
	/// <param name="response">The HTTP response associated with the exception.</param>
	/// <param name="targetObj">The target object associated with the error.</param>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
	public SonarrErrorRecord(SonarrHttpException exception, HttpResponseMessage? response, object? targetObj)
		: base(exception, exception.GetTypeName(), GetCategoryFromStatusCode(exception.StatusCode, out bool isIgnorable), targetObj)
	{
		ArgumentNullException.ThrowIfNull(exception);

		this.StatusCode = exception.StatusCode;
		this.ReasonPhrase = response?.ReasonPhrase;
		_headers = exception.Headers;
		this.IsIgnorable = isIgnorable;

		this.ErrorDetails = new ErrorDetails(GetMessage(exception, response, out string? requestUri))
		{
			RecommendedAction = "Examine the exception and act according to the returning StatusCode.",
		};

		this.RequestUri = requestUri;

		this.CategoryInfo.Activity = $"Sending {response?.RequestMessage?.Method.Method ?? "an"} HTTP request.";
		this.CategoryInfo.Reason = this.ReasonPhrase;
		this.CategoryInfo.TargetType = targetObj?.GetType().GetName();
	}

	/// <summary>
	/// Initializes a new <see cref="SonarrErrorRecord"/> instance from a Sonarr HTTP exception.
	/// </summary>
	/// <param name="response">The Sonarr HTTP exception.</param>
	public SonarrErrorRecord(SonarrHttpException response)
		: this(exception: response, response: response?.Response, (object?)null)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="SonarrErrorRecord"/> instance from a Sonarr HTTP exception.
	/// </summary>
	/// <param name="response">The Sonarr HTTP exception.</param>
	/// <param name="targetObj">The target object associated with the error.</param>
	public SonarrErrorRecord(SonarrHttpException response, object? targetObj)
		: this(exception: response, response: response?.Response, targetObj)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="SonarrErrorRecord"/> instance from a standard exception.
	/// </summary>
	/// <param name="normalEx">The exception to wrap.</param>
	/// <param name="errorId">The error identifier.</param>
	/// <param name="category">The error category.</param>
	/// <param name="targetObj">The target object associated with the error.</param>
	/// <param name="isIgnoreable"><see langword="true"/> when the error can be ignored; otherwise, <see langword="false"/>.</param>
	public SonarrErrorRecord(Exception normalEx, string errorId, ErrorCategory category, object? targetObj, bool isIgnoreable = false)
		: base(exception: normalEx, errorId, category, targetObj)
	{
		_headers = EmptyNameDictionary.Empty<string>();
		this.IsIgnorable = isIgnoreable;
	}

	/// <summary>
	/// Initializes a new <see cref="SonarrErrorRecord"/> instance by wrapping an existing error record.
	/// </summary>
	/// <param name="wraps">The error record to wrap.</param>
	public SonarrErrorRecord(ErrorRecord wraps)
		: base(wraps, wraps?.Exception)
	{
		_headers = EmptyNameDictionary.Empty<string>();
	}

	/// <summary>
	/// Gets a PowerShell error category from an HTTP status code.
	/// </summary>
	/// <param name="statusCode">The HTTP status code.</param>
	/// <param name="isIgnorable"><see langword="true"/> when the status should be considered ignorable; otherwise, <see langword="false"/>.</param>
	/// <returns>The mapped PowerShell error category.</returns>
	private static ErrorCategory GetCategoryFromStatusCode(HttpStatusCode? statusCode, out bool isIgnorable)
	{
		isIgnorable = false;
		switch (statusCode)
		{
			case HttpStatusCode.NotFound:
				isIgnorable = true;
				return ErrorCategory.ObjectNotFound;

			case HttpStatusCode.Unauthorized:
			case HttpStatusCode.Forbidden:
				return ErrorCategory.AuthenticationError;

			case HttpStatusCode.BadRequest:
				return ErrorCategory.InvalidData;

			case HttpStatusCode.BadGateway:
			case HttpStatusCode.ServiceUnavailable:
			case HttpStatusCode.GatewayTimeout:
			case HttpStatusCode.InternalServerError:
			case HttpStatusCode.RequestTimeout:
				return ErrorCategory.ConnectionError;

			case HttpStatusCode.MethodNotAllowed:
				return ErrorCategory.InvalidOperation;

			case HttpStatusCode.UnprocessableEntity:
				return ErrorCategory.InvalidArgument;

			case HttpStatusCode.Gone:
				return ErrorCategory.ResourceUnavailable;

			default:
				return ErrorCategory.NotSpecified;

		}
	}

	/// <summary>
	/// Parses HTTP response headers into a read-only dictionary.
	/// </summary>
	/// <param name="response">The response that provides headers.</param>
	/// <returns>A read-only header dictionary.</returns>
	private static IReadOnlyDictionary<string, string> ParseResponseHeaders(HttpResponseMessage response)
	{
		var headers = response.Headers;
		Dictionary<string, string> dict = new();
		foreach (var header in headers)
		{
			_ = dict.TryAdd(header.Key, string.Join(Environment.NewLine, header.Value));
		}

		return dict;
	}

	/// <summary>
	/// Builds the error message and resolves the request URI.
	/// </summary>
	/// <param name="exception">The exception providing base message text.</param>
	/// <param name="response">The HTTP response associated with the exception.</param>
	/// <param name="requestUri">The resolved request URI string.</param>
	/// <returns>The formatted error message.</returns>
	private static string GetMessage(Exception exception, HttpResponseMessage? response, out string? requestUri)
	{
		requestUri = response?.RequestMessage?.RequestUri?.ToString();
		if (string.IsNullOrWhiteSpace(requestUri))
		{
			return exception.Message;
		}

		return string.Create(
			length: requestUri.Length + 3 + exception.Message.Length,
			state: (requestUri, message: exception.Message),
			action: (chars, state) =>
			{
				state.requestUri.CopyTo(chars);
				int position = state.requestUri.Length;

				ReadOnlySpan<char> separator = stackalloc char[3] { ' ', '-', ' ' };
				separator.CopyTo(chars.Slice(position));
				position += separator.Length;

				state.message.CopyTo(chars.Slice(position));
			});
	}
}
