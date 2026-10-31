using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Services.Http.Handlers;
using MG.Sonarr.Next.Services.Http.Requests;
using System.Management.Automation;
using System.Net;

using AllowsNull = System.Diagnostics.CodeAnalysis.AllowNullAttribute;

namespace MG.Sonarr.Next.Services.Http;

/// <summary>
/// Represents the outcome of a request that a Sonarr API client sent: its status code, its URL, and the error when it
/// failed.
/// </summary>
/// <remarks>
/// <para>
/// A client returns a failure as an instance whose <see cref="IsError"/> is <see langword="true"/> and whose
/// <see cref="Error"/> describes it, rather than throwing.
/// </para>
/// <para>
/// A request that returns data gives a <see cref="SonarrClientResult{T}"/>, which adds the deserialized value.
/// </para>
/// </remarks>
[DebuggerDisplay(@"\{StatusCode = {StatusCode}, IsError = {IsError}\}")]
public class SonarrClientResult : ISonarrResponse, IHttpRequestUri
{
	/// <inheritdoc/>
	[MemberNotNullWhen(true, nameof(Error))]
	public virtual bool IsError { get; }

	/// <inheritdoc/>
	public virtual SonarrErrorRecord? Error { get; }

	/// <summary>
	/// Gets a value that indicates whether the result's status code is one that a caller can pass over without reporting.
	/// </summary>
	/// <remarks>
	/// Setting <see cref="StatusCode"/> sets this property as well.
	/// </remarks>
	/// <value>
	/// <see langword="true"/> if <see cref="StatusCode"/> is below 400 or is 404; otherwise, <see langword="false"/>.
	/// </value>
	public bool IsIgnoreable { get; private set; }

	/// <summary>
	/// Gets or sets the URL that the request was sent to.
	/// </summary>
	/// <remarks>
	/// Setting the property to <see langword="null"/> stores an empty string.
	/// </remarks>
	/// <value>The request's URL as a string, or an empty string when it isn't known.</value>
	[AllowsNull]
	[field: MaybeNull]
	public string RequestUrl
	{
		get => field ??= string.Empty;
		set => field = value ?? string.Empty;
	}

	/// <summary>
	/// Gets the HTTP status code of the response.
	/// </summary>
	/// <remarks>
	/// Initializing the property also sets <see cref="IsIgnoreable"/>.
	/// </remarks>
	/// <value>The status code that the server returned, or a code above 599 when no response was received.</value>
	public required HttpStatusCode StatusCode
	{
		get;
		init
		{
			field = value;
			this.IsIgnoreable = value is < HttpStatusCode.BadRequest or HttpStatusCode.NotFound;
		}
	}

	/// <inheritdoc/>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	string? IHttpRequestUri.RequestUri => this.RequestUrl;

	/// <summary>
	/// Initializes a new <see cref="SonarrClientResult"/> instance with no error, for a derived class that sets
	/// <see cref="StatusCode"/> itself.
	/// </summary>
	protected SonarrClientResult()
	{
	}
	/// <summary>
	/// Initializes a new <see cref="SonarrClientResult"/> instance that represents a successful response, with the status
	/// code and URL of the specified response.
	/// </summary>
	/// <param name="response">The response that the server returned. This value must not be <see langword="null"/>.</param>
	/// <param name="requestUri">
	/// The URL to store when <paramref name="response"/> doesn't carry its request's URI, or <see langword="null"/>.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="response"/> is null.</exception>
	[SetsRequiredMembers]
	public SonarrClientResult(HttpResponseMessage response, string? requestUri = null)
	{
		ArgumentNullException.ThrowIfNull(response);
		this.StatusCode = response.StatusCode;
		this.RequestUrl = response.RequestMessage?.RequestUri?.ToString() ?? requestUri;
	}
	/// <summary>
	/// Initializes a new <see cref="SonarrClientResult"/> instance that represents a failure, with the specified error
	/// record.
	/// </summary>
	/// <remarks>
	/// The result takes its URL and status code from <paramref name="error"/>, and a status code of 600 when the record
	/// has none.
	/// </remarks>
	/// <param name="error">The record that describes the failure. This value must not be <see langword="null"/>.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
	[SetsRequiredMembers]
	public SonarrClientResult(SonarrErrorRecord error)
	{
		ArgumentNullException.ThrowIfNull(error);
		this.Error = error;
		this.IsError = true;
		this.RequestUrl = error.RequestUri;
		this.StatusCode = error.StatusCode ?? ErrorHandler.NoResponseCode;
	}

	/// <summary>
	/// Creates a successful result that holds the specified value and status code.
	/// </summary>
	/// <remarks>
	/// When <paramref name="value"/> is an <see cref="IHttpRequestUri"/> with a URI, the result takes its URL from it
	/// rather than from <paramref name="uriPath"/>.
	/// </remarks>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value that the request returned. This value must not be <see langword="null"/>.</param>
	/// <param name="code">The HTTP status code of the response.</param>
	/// <param name="uriPath">The URL that the request was sent to, or <see langword="null"/> when it isn't known.</param>
	/// <returns>A result that holds <paramref name="value"/> and <paramref name="code"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static SonarrClientResult<T> Create<T>([DisallowNull] T value, HttpStatusCode code, string? uriPath = null)
	{
		string? requestUri = value is IHttpRequestUri hasReqUri
			? hasReqUri.RequestUri ?? uriPath
			: uriPath;

		return new SonarrClientResult<T>(value)
		{
			StatusCode = code,
			RequestUrl = requestUri,
		};
	}
	/// <summary>
	/// Creates a successful result that holds the specified value, with the status code and URL of the specified response.
	/// </summary>
	/// <remarks>
	/// The result takes its URL from <paramref name="value"/> when it's an <see cref="IHttpRequestUri"/> with a URI, then
	/// from the request of <paramref name="response"/>, and last from <paramref name="uriPath"/>.
	/// </remarks>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value that the request returned. This value must not be <see langword="null"/>.</param>
	/// <param name="response">The response that the value was read from. This value must not be <see langword="null"/>.</param>
	/// <param name="uriPath">The URL to use when neither <paramref name="value"/> nor <paramref name="response"/> gives one.</param>
	/// <returns>A result that holds <paramref name="value"/> and the status code of <paramref name="response"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static SonarrClientResult<T> Create<T>(T value, HttpResponseMessage response, string? uriPath = null)
	{
		string? requestUri = value is IHttpRequestUri hasReqUri
			? hasReqUri.RequestUri ?? response.RequestMessage?.RequestUri?.ToString() ?? uriPath
			: response.RequestMessage?.RequestUri?.ToString() ?? uriPath;

		return new(value)
		{
			StatusCode = response.StatusCode,
			RequestUrl = requestUri,
		};
	}
	/// <summary>
	/// Creates a failed result from the specified exception, error category, and status code.
	/// </summary>
	/// <remarks>
	/// The result's error record takes the exception's type name as its error ID, and its URL from the exception when it's
	/// an <see cref="IHttpRequestUri"/> with a URI, or else from the request of <paramref name="response"/>.
	/// </remarks>
	/// <param name="exception">The exception that made the request fail. This value must not be <see langword="null"/>.</param>
	/// <param name="errorCategory">The category of the error.</param>
	/// <param name="statusCode">The status code of the result.</param>
	/// <param name="response">The response that the server returned, or <see langword="null"/> when there was none.</param>
	/// <returns>A result whose <see cref="IsError"/> is <see langword="true"/>.</returns>
	public static SonarrClientResult FromException(Exception exception, ErrorCategory errorCategory, HttpStatusCode statusCode, HttpResponseMessage? response = null)
	{
		string name = exception.GetTypeName();

		string? requestUri = exception is IHttpRequestUri hasReqUri
			? hasReqUri.RequestUri ?? response?.RequestMessage?.RequestUri?.ToString()
			: response?.RequestMessage?.RequestUri?.ToString();

		SonarrErrorRecord error = new(exception, name, errorCategory, requestUri);
		return new SonarrClientResult(error)
		{
			StatusCode = statusCode,
		};
	}
	/// <summary>
	/// Creates a failed result for a request that would have returned a value, from the specified exception, error
	/// category, and status code.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A <see cref="SonarrHttpException"/> goes to
	/// <see cref="FromException{T}(SonarrHttpException, ErrorCategory, HttpStatusCode, HttpResponseMessage?)"/>, whose
	/// error record carries the details of the HTTP exchange.
	/// </para>
	/// <para>
	/// For any other exception, the result's error record takes the exception's type name as its error ID, and its URL
	/// from the exception when it's an <see cref="IHttpRequestUri"/> with a URI, or else from the request of
	/// <paramref name="response"/>.
	/// </para>
	/// </remarks>
	/// <typeparam name="T">The type of the value that the request would have returned.</typeparam>
	/// <param name="exception">The exception that made the request fail. This value must not be <see langword="null"/>.</param>
	/// <param name="errorCategory">The category of the error.</param>
	/// <param name="statusCode">The status code of the result.</param>
	/// <param name="response">The response that the server returned, or <see langword="null"/> when there was none.</param>
	/// <returns>A result whose <see cref="IsError"/> is <see langword="true"/>.</returns>
	public static SonarrClientResult<T> FromException<T>(Exception exception, ErrorCategory errorCategory, HttpStatusCode statusCode, HttpResponseMessage? response = null)
	{
		if (exception is SonarrHttpException reqEx)
			return FromException<T>(reqEx, errorCategory, statusCode, response);

		string name = exception.GetTypeName();

		string? requestUri = exception is IHttpRequestUri hasReqUri
			? hasReqUri.RequestUri ?? response?.RequestMessage?.RequestUri?.ToString()
			: response?.RequestMessage?.RequestUri?.ToString();

		SonarrErrorRecord error = new(exception, name, errorCategory, requestUri);
		return new SonarrClientResult<T>(error)
		{
			StatusCode = statusCode,
		};
	}
	/// <summary>
	/// Creates a failed result for a request that would have returned a value, from the specified Sonarr HTTP exception.
	/// </summary>
	/// <remarks>
	/// The result's error record holds the exception's status code, its reason phrase, its headers, and a message that
	/// names the request's URL.
	/// </remarks>
	/// <typeparam name="T">The type of the value that the request would have returned.</typeparam>
	/// <param name="exception">The exception that made the request fail. This value must not be <see langword="null"/>.</param>
	/// <param name="errorCategory">The category of the error.</param>
	/// <param name="statusCode">The status code of the result.</param>
	/// <param name="response">The response that the server returned, or <see langword="null"/> when there was none.</param>
	/// <returns>A result whose <see cref="IsError"/> is <see langword="true"/>.</returns>
	public static SonarrClientResult<T> FromException<T>(SonarrHttpException exception, ErrorCategory errorCategory, HttpStatusCode statusCode, HttpResponseMessage? response = null)
	{
		SonarrErrorRecord error = new(exception, response, exception.RequestUri);

		return new SonarrClientResult<T>(error);
	}

	/// <summary>
	/// The error record of every result that <see cref="NotFound{T}"/> creates.
	/// </summary>
	private static readonly SonarrErrorRecord _notFound = new(new Exception("Not found."), "NotFound", ErrorCategory.ObjectNotFound, targetObj: null, isIgnoreable: true);

	/// <summary>
	/// Creates a failed result for a request whose resource wasn't found.
	/// </summary>
	/// <typeparam name="T">The type of the value that the request would have returned.</typeparam>
	/// <returns>
	/// A result whose <see cref="IsError"/> and <see cref="IsIgnoreable"/> are <see langword="true"/>, whose
	/// <see cref="StatusCode"/> is <see cref="HttpStatusCode.NotFound"/>, and whose error record has the category
	/// <see cref="ErrorCategory.ObjectNotFound"/>.
	/// </returns>
	[SuppressMessage("Style", "IDE0022")]
	public static SonarrClientResult<T> NotFound<T>() => new(_notFound)
	{
		IsIgnoreable = true,
		StatusCode = HttpStatusCode.NotFound,
	};
}

/// <summary>
/// Represents the outcome of a request that a Sonarr API client sent, with the value that a successful request returned.
/// </summary>
/// <typeparam name="T">The type of the value that the request returns.</typeparam>
[DebuggerDisplay(@"\{StatusCode = {StatusCode}, IsError = {IsError}, Value = {Value}\}")]
public sealed class SonarrClientResult<T> : SonarrClientResult
{
	/// <inheritdoc/>
	public override SonarrErrorRecord? Error => base.Error;

	/// <summary>
	/// Gets a value that indicates whether the request failed.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the request failed and <see cref="Error"/> describes why; otherwise,
	/// <see langword="false"/>, and <see cref="Value"/> holds the value that the request returned.
	/// </value>
	[MemberNotNullWhen(true, nameof(Error)), MemberNotNullWhen(false, nameof(Value))]
	public override bool IsError => base.IsError;

	/// <summary>
	/// Gets the value that the request returned.
	/// </summary>
	/// <value>
	/// The deserialized value, or the default of <typeparamref name="T"/> when <see cref="IsError"/> is
	/// <see langword="true"/>.
	/// </value>
	public T? Value { get; }

	/// <summary>
	/// Initializes a new <see cref="SonarrClientResult{T}"/> instance that represents a success, with the specified value.
	/// </summary>
	/// <param name="value">The value that the request returned. This value must not be <see langword="null"/>.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public SonarrClientResult(T value) : base()
	{
		ArgumentNullException.ThrowIfNull(value);
		this.Value = value;
	}
	/// <summary>
	/// Initializes a new <see cref="SonarrClientResult{T}"/> instance that represents a failure, with the specified error
	/// record.
	/// </summary>
	/// <remarks>
	/// The result takes its URL and status code from <paramref name="error"/>, and a status code of 600 when the record
	/// has none.
	/// </remarks>
	/// <param name="error">The record that describes the failure. This value must not be <see langword="null"/>.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
	[SetsRequiredMembers]
	public SonarrClientResult(SonarrErrorRecord error) : base(error)
	{
		this.Value = default;
	}

	/// <summary>
	/// Determines whether <see cref="Value"/> can take a metadata tag.
	/// </summary>
	/// <param name="taggable">
	/// When this method returns, <see cref="Value"/> as an <see cref="IJsonMetadataTaggable"/>, or
	/// <see langword="null"/> when it isn't one.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <see cref="Value"/> is an <see cref="IJsonMetadataTaggable"/>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public bool IsDataTaggable([NotNullWhen(true)] out IJsonMetadataTaggable? taggable)
	{
		if (this.Value is IJsonMetadataTaggable jsonMetadataTaggable)
		{
			taggable = jsonMetadataTaggable;
			return true;
		}

		taggable = null;
		return false;
	}

	/// <summary>
	/// Determines whether <see cref="Value"/> can be sorted.
	/// </summary>
	/// <param name="sortable">
	/// When this method returns, <see cref="Value"/> as an <see cref="ISortable"/>, or <see langword="null"/> when it
	/// isn't one.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <see cref="Value"/> is an <see cref="ISortable"/>; otherwise, <see langword="false"/>.
	/// </returns>
	public bool IsDataSortable([NotNullWhen(true)] out ISortable? sortable)
	{
		if (this.Value is ISortable sort)
		{
			sortable = sort;
			return true;
		}

		sortable = null;
		return false;
	}
}
