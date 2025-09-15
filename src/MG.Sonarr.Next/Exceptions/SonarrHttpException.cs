using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using System.Net;

namespace MG.Sonarr.Next.Exceptions
{
    /// <summary>
    /// An exception class thrown when an <see cref="HttpClient"/> exception is thrown from a client
    /// implementation written for PoshSonarr.
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
        public IErrorCollection ExtendedInfo { get; }
        /// <summary>
        /// The headers from the <see cref="HttpResponseMessage"/> that generated this exception.
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers { get; }
        /// <summary>
        /// Indicates whether the response's status code was <see cref="HttpStatusCode.NotFound"/>.
        /// </summary>
        public bool IsNotFound => HttpStatusCode.NotFound == this.StatusCode;
        /// <summary>
        /// Gets the status code of the response that generated the exception.
        /// </summary>
        public HttpStatusCode? StatusCode { get; }
        /// <summary>
        /// The response message that generated the current exception.
        /// </summary>
        public HttpResponseMessage? Response { get; }
        /// <summary>
        /// Gets the reason phrase which is typically is sent by servers together with the status code
        /// from the response that generated the current exception.
        /// </summary>
        public string? ReasonPhrase { get; }
        /// <summary>
        /// The <see cref="string"/> representation of the <see cref="Uri"/> of the request message or the
        /// original path provided that generated the current exception.
        /// </summary>
        public string? RequestUri { get; }

        [DebuggerStepThrough]
        public SonarrHttpException(HttpRequestMessage request, HttpResponseMessage? response)
            : this(request, response, ErrorCollection.Empty, null)
        {
        }
        [DebuggerStepThrough]
        public SonarrHttpException(HttpRequestMessage request, HttpResponseMessage? response, IErrorCollection errors)
            : this(request, response, errors, null)
        {
        }
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

        private static string GetMessage(HttpRequestMessage request, HttpResponseMessage? response, Exception? inner, IErrorCollection errors, out string? requestUri)
        {
            requestUri = GetRequestUri(request);
            string details = errors.Count <= 0
                ? inner is null
                    ? "An exception occurred in response"
                    : inner.GetBaseException().Message
                : errors.BuildMessage();

            return response is not null
                ? $"{response.StatusCode.ToResponseString()}: {details}".TrimEnd().TrimEnd(':')
                : details;
        }
        private static string? GetRequestUri(HttpRequestMessage request)
        {
            return request.RequestUri?.ToString();
        }
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
}