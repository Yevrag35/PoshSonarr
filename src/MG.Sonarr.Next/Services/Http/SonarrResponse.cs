using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json;
using System.Management.Automation;
using System.Net;

namespace MG.Sonarr.Next.Services.Http
{
    /// <summary>
    /// An interface that represents HTTP responses from Sonarr API endpoints.
    /// </summary>
    public interface ISonarrResponse
    {
        /// <summary>
        /// Gets the error record of the response, if any.
        /// </summary>
        SonarrErrorRecord? Error { get; }

        /// <summary>
        /// Gets whether the response is considered an error.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Error))]
        bool IsError { get; }
        /// <summary>
        /// Gets the HTTP status code of the response.
        /// </summary>
        HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Gets the <see cref="string"/>-form of the URL that the response's request was sent to.
        /// </summary>
        string RequestUrl { get; }
    }
    /// <summary>
    /// An interface for <see cref="ISonarrResponse"/> implementations that were measured and provide a
    /// <see cref="TimeSpan"/> of the elapsed time.
    /// </summary>
    public interface ISonarrTimedResponse : ISonarrResponse
    {
        /// <summary>
        /// Gets the amount of time between when the request was sent and when the response was received.
        /// </summary>
        TimeSpan Elapsed { get; }
        /// <summary>
        /// Gets a scoped <see cref="IServiceProvider"/> for use by code that is using this response.
        /// </summary>
        IServiceProvider Services { get; }
    }

    /// <summary>
    /// A struct object that encapsulates the response from a Sonarr API endpoint.
    /// </summary>
    public readonly struct SonarrResponse : ISonarrResponse
    {
        readonly SonarrErrorRecord? _error;
        readonly bool _isError;
        readonly bool _isNotEmpty;
        readonly HttpStatusCode _statusCode;
        readonly string? _url;

        /// <inheritdoc cref="ISonarrResponse.Error"/>
        public SonarrErrorRecord? Error => _error;

        /// <summary>
        /// Gets whether this response object is empty - representing the default value of the struct.
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if this response is empty, also meaning that <see cref="Error"/> 
        ///     is <see langword="null"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public bool IsEmpty => !_isNotEmpty;

        /// <inheritdoc cref="ISonarrResponse.IsError"/>
        /// <returns>
        /// <see langword="true"/> if the response is determined to be an error result, also meaning that
        /// <see cref="Error"/> is *not* <see langword="null"/>; otherwise, <see langword="false"/>.
        /// </returns>
        [MemberNotNullWhen(true, nameof(_error), nameof(Error))]
        public bool IsError => _isError;

        /// <inheritdoc cref="ISonarrResponse.RequestUrl"/>
        /// <remarks>
        /// This value will never be <see langword="null"/>.
        /// </remarks>
        /// <returns>
        /// The URL string of the request that produced this response or, if not provided, an empty string.
        /// </returns>
        public string RequestUrl => _url ?? string.Empty;

        /// <inheritdoc cref="ISonarrResponse.StatusCode"/>
        public HttpStatusCode StatusCode => _statusCode;

        private SonarrResponse(string url, HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
            _error = null;
            _isError = false;
            _url = url ?? string.Empty;
        }
        private SonarrResponse(string url, SonarrErrorRecord record, HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
            _error = record;
            _isError = true;
            _url = url ?? string.Empty;
        }

        /// <summary>
        /// Creates a successful response object from the referenced <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <remarks>
        /// If the request URL cannot be determined from <paramref name="message"/>, then 
        /// <paramref name="uriPath"/> will be used instead.
        /// </remarks>
        /// <param name="message">A reference to the successful HTTP response.</param>
        /// <param name="uriPath">
        /// The relative URI path of the request that generated the 
        /// successful response. Used if the request URI from <paramref name="message"/>
        /// cannot be constructed.
        /// </param>
        /// <returns>
        /// A <see cref="SonarrResponse"/> object whose <see cref="SonarrResponse.IsError"/> property is
        /// <see langword="false"/> - indicating a successful response.
        /// </returns>
        public static SonarrResponse Create(HttpResponseMessage message, string uriPath)
        {
            uriPath = message.RequestMessage?.RequestUri?.ToString() ?? uriPath;

            return new SonarrResponse(uriPath, message.StatusCode);
        }

        /// <summary>
        /// Produces an response object from the provided <see cref="SonarrErrorRecord"/>, which
        /// is always considered an error.
        /// </summary>
        /// <param name="error">The error record to create the failed response from.</param>
        /// <returns>
        /// A <see cref="SonarrResponse"/> object that encapsulates the failed response and whose
        /// <see cref="IsError"/> property is <see langword="true"/>.
        /// </returns>
        public static SonarrResponse FromException(SonarrErrorRecord error)
        {
            return new SonarrResponse(error.RequestUri ?? string.Empty, error, error.StatusCode.GetValueOrDefault());
        }

        /// <inheritdoc cref="FromException(SonarrErrorRecord)" path="/*[not(self::returns)]"/>
        /// <typeparam name="T"><inheritdoc cref="SonarrResponse{T}"/></typeparam>
        /// <returns>
        /// A <see cref="SonarrResponse{T}"/> object that encapsulates the failed response and whose
        /// <see cref="SonarrResponse{T}.IsError"/> property is <see langword="true"/>.
        /// </returns>
        public static SonarrResponse<T> FromException<T>(SonarrErrorRecord error)
        {
            return new SonarrResponse<T>(error.RequestUri ?? string.Empty, default, error, error.StatusCode.GetValueOrDefault());
        }

        /// <inheritdoc cref="FromException{T}(string, Exception, ErrorCategory, HttpStatusCode, HttpResponseMessage?)"
        ///     path="/*[not(self::returns)]"/>
        /// <inheritdoc cref="FromException(SonarrErrorRecord)" path="/returns"/>
        public static SonarrResponse FromException(string url, Exception exception, ErrorCategory category, HttpStatusCode statusCode, HttpResponseMessage? response = null)
        {
            string name = exception.GetTypeName();
            SonarrErrorRecord record = exception is SonarrHttpException reqEx
                ? new SonarrErrorRecord(reqEx, response, url)
                : new SonarrErrorRecord(exception, name, category, url);
            return new SonarrResponse(url, record, statusCode);
        }

        /// <inheritdoc cref="FromException{T}(SonarrErrorRecord)" path="/*[not(self::summary)]"/>
        /// <inheritdoc cref="SonarrResponse{T}(string, T, SonarrErrorRecord, HttpStatusCode)"
        ///     path="/param"/>
        /// <summary>
        /// Produces an failed response object from the provided request URL, exception reference that is 
        /// the cause of this failed responsee, the <see cref="ErrorCategory"/> of the error record, 
        /// the HTTP status code of the response, and, optionally, a reference to the raw HTTP response.
        /// </summary>
        /// <param name="exception">The exception that is the cause of this failed response.</param>
        /// <param name="category">
        ///     The error category of to use in the <see cref="SonarrErrorRecord"/>
        ///     for this response.
        /// </param>
        /// <param name="response">
        ///     A reference to the raw HTTP response that is the cause of this failed response.
        /// </param>
        public static SonarrResponse<T> FromException<T>(string url, Exception exception, ErrorCategory category, HttpStatusCode statusCode, HttpResponseMessage? response = null)
        {
            string name = exception.GetTypeName();
            SonarrErrorRecord record = exception is SonarrHttpException reqEx
                ? new SonarrErrorRecord(reqEx, response ?? reqEx.Response, url)
                : new SonarrErrorRecord(exception, name, category, url);
            return new SonarrResponse<T>(url, default, record, statusCode);
        }
    }

    /// <summary><inheritdoc cref="SonarrResponse"/> Also returning the deserialized data from the 
    /// response as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the deserialized data returned in the response.</typeparam>
    public readonly struct SonarrResponse<T> : ISonarrResponse
    {
        readonly T? _data;
        readonly string? _url;
        readonly SonarrErrorRecord? _error;
        readonly bool _isError;
        readonly bool _isNotEmpty;
        readonly HttpStatusCode _statusCode;

        /// <summary>
        /// Gets the deserialized data returned from the response.
        /// </summary>
        /// <remarks>
        /// This can be <see langword="null"/> if <typeparamref name="T"/> is nullable.
        /// </remarks>
        public T? Data => _data;
        public SonarrErrorRecord? Error => _error;
        
        /// <inheritdoc cref="SonarrResponse.IsEmpty" path="/*[not(self::returns)]"/>
        /// <returns>
        ///     <see langword="true"/> if this response is empty, also meaning that <see cref="Data"/>
        ///     and <see cref="Error"/> are <see langword="null"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public bool IsEmpty => !_isNotEmpty;

        /// <inheritdoc cref="ISonarrResponse.IsError"/>
        /// <returns>
        /// <see langword="true"/> if the response is determined to be an error result, also meaning that
        /// <see cref="Error"/> is *not* <see langword="null"/>; otherwise, <see langword="false"/>.
        /// </returns>
        [MemberNotNullWhen(true, nameof(Error))]
        [MemberNotNullWhen(false, nameof(Data))]
        public bool IsError => _isError;

        /// <inheritdoc cref="SonarrResponse.RequestUrl"/>
        public string RequestUrl => _url ?? string.Empty;

        /// <inheritdoc cref="ISonarrResponse.StatusCode"/>
        public HttpStatusCode StatusCode => _statusCode;

        /// <summary>
        /// Initializes a new instance of <see cref="SonarrResponse{T}"/> with the provided request URL, 
        /// deserialized response data, <see cref="SonarrErrorRecord"/> exception, and the HTTP status code
        /// of the response.
        /// </summary>
        /// <param name="url">The request URL that produced this response.</param>
        /// <param name="data">The deserialized body of the response, if any.</param>
        /// <param name="error">The error encountered during the response, if any.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        public SonarrResponse(string? url, T? data, SonarrErrorRecord? error, HttpStatusCode statusCode)
        {
            _data = data;
            _statusCode = statusCode;
            _url = url ?? string.Empty;
            _error = error;
            bool isError = error is not null;
            _isNotEmpty = isError || data is not null;
            _isError = isError;
        }

        internal bool IsDataTaggable([NotNullWhen(true)] out IJsonMetadataTaggable? taggable)
        {
            if (_data is IJsonMetadataTaggable metadata)
            {
                taggable = metadata;
                return true;
            }

            taggable = default;
            return false;
        }

        internal bool IsDataSortable([NotNullWhen(true)] out ISortable? sortable)
        {
            if (_data is ISortable sort)
            {
                sortable = sort;
                return true;
            }

            sortable = default;
            return false;
        }

        public static implicit operator SonarrResponse<T>(ValueTuple<string?, HttpStatusCode, T> success)
        {
            return new(success.Item1, success.Item3, null, success.Item2);
        }
    }

    internal static class HttpResponseMessageExtensions
    {
        internal static SonarrResponse<T> ToResult<T>(this HttpResponseMessage message, string path, T? data)
        {
            string url = message.RequestMessage?.RequestUri?.ToString() ?? path;
            if (data is null)
            {
                return SonarrResponse.FromException<T>(
                    url, new EmptyHttpResponseException(url), ErrorCategory.InvalidResult, message.StatusCode);
            }

            return new SonarrResponse<T>(url, data, null, message.StatusCode);
        }
    }
}
