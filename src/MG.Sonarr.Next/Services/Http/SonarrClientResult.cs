using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Guarding;
using System.Management.Automation;
using System.Net;

using AllowsNull = System.Diagnostics.CodeAnalysis.AllowNullAttribute;

namespace MG.Sonarr.Next.Services.Http
{
    /// <summary>
    /// Represents the result of a Sonarr API client operation, including HTTP status, error information, and request
    /// details.
    /// </summary>
    /// <remarks>This class provides a unified way to access the outcome of Sonarr API requests, including
    /// both successful and error responses. Use the properties to inspect the HTTP status code, determine if an error
    /// occurred, and retrieve error details when available. For generic results containing a value, use the derived
    /// <see cref="SonarrClientResult{T}"/>.</remarks>
    [DebuggerDisplay(@"\{StatusCode = {StatusCode}, IsError = {IsError}\}")]
    public class SonarrClientResult : ISonarrResponse, IHttpRequestUri
    {
        /// <inheritdoc/>
        [MemberNotNullWhen(true, nameof(Error))]
        public virtual bool IsError { get; }

        /// <inheritdoc/>
        public virtual SonarrErrorRecord? Error { get; }

        /// <inheritdoc/>
        [AllowsNull][field: MaybeNull]
        public string RequestUrl
        {
            get => field ??= string.Empty;
            set => field = value ?? string.Empty;
        }

        /// <inheritdoc/>
        public required HttpStatusCode StatusCode { get; init; }

        /// <inheritdoc/>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        string? IHttpRequestUri.RequestUri => this.RequestUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="SonarrClientResult"/> class.
        /// </summary>
        protected SonarrClientResult()
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SonarrClientResult"/> class using the specified HTTP response message.
        /// </summary>
        /// <param name="response">The HTTP response message received from the Sonarr API. Cannot be null.</param>
        /// <exception cref="ArgumentNullException"><paramref name="response"/> is null.</exception>
        [SetsRequiredMembers]
        public SonarrClientResult(HttpResponseMessage response)
        {
            ArgumentNullException.ThrowIfNull(response);
            this.StatusCode = response.StatusCode;
            this.RequestUrl = response.RequestMessage?.RequestUri?.ToString() ?? string.Empty;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SonarrClientResult"/> class that represents an error response from the Sonarr
        /// API.
        /// </summary>
        /// <param name="error">The error record containing details about the API error.</param>
        /// <exception cref="ArgumentNullException"><paramref name="error"/> is null.</exception>
        [SetsRequiredMembers]
        public SonarrClientResult(SonarrErrorRecord error)
        {
            ArgumentNullException.ThrowIfNull(error);
            this.Error = error;
            this.IsError = true;
            this.RequestUrl = error.RequestUri;
            this.StatusCode = error.StatusCode ?? (HttpStatusCode)599;
        }

        /// <summary>
        /// Creates a new instance of <see cref="SonarrClientResult{T}"/> with the specified value, HTTP status code,
        /// and optional request URL path.
        /// </summary>
        /// <typeparam name="T">The type of the value to be encapsulated in the result.</typeparam>
        /// <param name="value">The value to be included in the result. Cannot be null.</param>
        /// <param name="code">The HTTP status code to associate with the result.</param>
        /// <param name="uriPath">The relative URL path of the request, or <see langword="null"/> if not applicable.</param>
        /// <returns>A <see cref="SonarrClientResult{T}"/> containing the specified value, status code, and request URL path.</returns>
        /// <inheritdoc cref="SonarrClientResult{T}.SonarrClientResult(T)" path="/exception"/>
        public static SonarrClientResult<T> Create<T>([DisallowNull] T value, HttpStatusCode code, string? uriPath = null)
        {
            return new SonarrClientResult<T>(value)
            {
                StatusCode = code,
                RequestUrl = uriPath,
            };
        }
        /// <summary>
        /// Creates a new <see cref="SonarrClientResult{T}"/> instance containing the specified value and HTTP response
        /// information.
        /// </summary>
        /// <remarks>If <paramref name="response"/> does not contain a request URI, <paramref
        /// name="uriPath"/> is used to populate the request URL in the result.</remarks>
        /// <typeparam name="T">The type of the value to be encapsulated in the result.</typeparam>
        /// <param name="value">The value to be included in the result. Cannot be null.</param>
        /// <param name="response">The HTTP response message associated with the result. Used to set status and request URL information.</param>
        /// <param name="uriPath">An optional URI path to use as the request URL if not available from <paramref name="response"/>.</param>
        /// <returns>A <see cref="SonarrClientResult{T}"/> containing the provided value and details from the HTTP response.</returns>
        public static SonarrClientResult<T> Create<T>([DisallowNull] T value, HttpResponseMessage response, string? uriPath = null)
        {
            return new(value)
            {
                StatusCode = response.StatusCode,
                RequestUrl = response.RequestMessage?.RequestUri?.ToString() ?? uriPath,
            };
        }
        /// <summary>
        /// Creates a new <see cref="SonarrClientResult"/> instance representing an error condition based on the
        /// specified exception and error details.
        /// </summary>
        /// <remarks>Use this method to convert exceptions and related HTTP response data into a
        /// standardized error result for client operations.</remarks>
        /// <param name="exception">The exception that caused the error. Cannot be null.</param>
        /// <param name="errorCategory">The category that classifies the type of error encountered.</param>
        /// <param name="statusCode">The HTTP status code associated with the error response.</param>
        /// <param name="response">The HTTP response message related to the error, if available; otherwise, <see langword="null"/>.</param>
        /// <returns>A <see cref="SonarrClientResult"/> containing error information derived from the provided exception and
        /// details.</returns>
        public static SonarrClientResult FromException(Exception exception, ErrorCategory errorCategory, HttpStatusCode statusCode, HttpResponseMessage? response = null)
        {
            string name = exception.GetTypeName();
            SonarrErrorRecord error = new(exception, name, errorCategory, response?.RequestMessage?.RequestUri?.ToString());
            return new SonarrClientResult(error);
        }
        /// <summary>
        /// Creates a new <see cref="SonarrClientResult{T}"/> representing a failed operation due to the specified
        /// exception.
        /// </summary>
        /// <remarks>If the exception is a <see cref="SonarrHttpException"/>, additional details from the
        /// HTTP context are included in the result. Use this method to standardize error handling for client
        /// operations.</remarks>
        /// <typeparam name="T">The type of the result value associated with the client operation.</typeparam>
        /// <param name="exception">The exception that caused the operation to fail.</param>
        /// <param name="errorCategory">The category that classifies the error for diagnostic or handling purposes.</param>
        /// <param name="statusCode">The HTTP status code associated with the failed operation.</param>
        /// <param name="response">The HTTP response message related to the failed request, if available; otherwise, <see langword="null"/>.</param>
        /// <returns>A <see cref="SonarrClientResult{T}"/> containing error information derived from the provided exception.</returns>
        public static SonarrClientResult<T> FromException<T>(Exception exception, ErrorCategory errorCategory, HttpStatusCode statusCode, HttpResponseMessage? response = null)
        {
            if (exception is SonarrHttpException reqEx)
                return FromException<T>(reqEx, errorCategory, statusCode, response);

            string name = exception.GetTypeName();
            SonarrErrorRecord error = new(exception, name, errorCategory, response?.RequestMessage?.RequestUri?.ToString());
            return new SonarrClientResult<T>(error);
        }
        /// <summary>
        /// Creates a new result representing a failed operation due to a Sonarr HTTP exception.
        /// </summary>
        /// <typeparam name="T">The type of the value that would have been returned if the operation had succeeded.</typeparam>
        /// <param name="exception">The Sonarr HTTP exception that caused the operation to fail.</param>
        /// <param name="errorCategory">The category that classifies the type of error encountered.</param>
        /// <param name="statusCode">The HTTP status code associated with the failed request.</param>
        /// <param name="response">The HTTP response message returned by the server, if available; otherwise, <see langword="null"/>.</param>
        /// <returns>A <see cref="SonarrClientResult{T}"/> containing error information about the failed operation.</returns>
        public static SonarrClientResult<T> FromException<T>(SonarrHttpException exception, ErrorCategory errorCategory, HttpStatusCode statusCode, HttpResponseMessage? response = null)
        {
            string name = exception.GetTypeName();
            SonarrErrorRecord error = new(exception, response, exception.RequestUri);

            return new SonarrClientResult<T>(error);
        }
    }

    [DebuggerDisplay(@"\{StatusCode = {StatusCode}, IsError = {IsError}, Value = {Value}\}")]
    public sealed class SonarrClientResult<T> : SonarrClientResult
    {
        /// <inheritdoc/>
        public override SonarrErrorRecord? Error { get; }

        /// <inheritdoc/>
        [MemberNotNullWhen(true, nameof(Error)), MemberNotNullWhen(false, nameof(Value))]
        public override bool IsError { get; }

        /// <summary>
        /// Gets the parsed value from the HTTP response, or <see langword="null"/> if <see cref="IsError"/> is <see langword="true"/>.
        /// </summary>
        public T? Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SonarrClientResult{T}"/> class with the specified value.
        /// </summary>
        /// <param name="value">The result value to be encapsulated by the client result.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        public SonarrClientResult([DisallowNull] T value) : base()
        {
            ArgumentNullException.ThrowIfNull(value);
            this.Value = value;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SonarrClientResult{T}"/> class with the specified error information.
        /// </summary>
        /// <param name="error">The error record that describes the failure encountered by the client.</param>
        /// <inheritdoc path="/exception"/>
        [SetsRequiredMembers]
        public SonarrClientResult(SonarrErrorRecord error) : base(error)
        {
            this.Value = default;
        }
    }
}
