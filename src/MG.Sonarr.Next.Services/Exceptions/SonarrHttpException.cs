using MG.Sonarr.Next.Services.Collections;
using MG.Sonarr.Next.Services.Exceptions;
using MG.Sonarr.Next.Services.Extensions.PSO;
using MG.Sonarr.Next.Services.Models;
using System.Management.Automation;
using System.Net;
using System.Runtime.Serialization;

namespace MG.Sonarr.Next.Exceptions
{
    /// <summary>
    /// An exception class thrown when an <see cref="HttpClient"/> exception is thrown from a client 
    /// implementation written for PoshSonarr.
    /// </summary>
    [Serializable]
    public sealed class SonarrHttpException : PoshSonarrException
    {
        /// <summary>
        /// The stack trace, if any, received from the Sonarr server.
        /// </summary>
        public string? Description { get; }
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

        public SonarrHttpException(HttpRequestMessage request, HttpResponseMessage? response, PSObject? responseContent, Exception? innerException)
            : base(GetMessage(request, innerException, responseContent, out string? reqUri), innerException)
        {
            this.Description = GetDescription(responseContent);
            this.RequestUri = reqUri;
            this.Response = response;
            this.ReasonPhrase = response?.ReasonPhrase;
            this.StatusCode = response?.StatusCode;
            this.Headers = ParseResponseHeaders(response);
        }
        public SonarrHttpException(HttpRequestMessage request, HttpResponseMessage? response, SonarrServerError? serverError, Exception? innerException)
            : base(GetMessage(request, innerException, serverError, out string? reqUri), innerException)
        {
            this.Description = serverError?.Description;
            this.RequestUri = reqUri;
            this.Response = response;
            this.ReasonPhrase = response?.ReasonPhrase;
            this.StatusCode = response?.StatusCode;
            this.Headers = ParseResponseHeaders(response);
        }

        private SonarrHttpException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // Retrieve properties/fields from the serialization store.
            this.Headers = (IReadOnlyDictionary<string, string>?)info.GetValue(nameof(this.Headers), typeof(Dictionary<string, string>)) ?? EmptyNameDictionary.Default;
            this.StatusCode = (HttpStatusCode?)info.GetValue(nameof(this.StatusCode), typeof(HttpStatusCode?));
            this.ReasonPhrase = (string?)info.GetValue(nameof(this.ReasonPhrase), typeof(string));
            this.RequestUri = (string?)info.GetValue(nameof(this.RequestUri), typeof(string));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            // Add public properties/fields to the serialization store.
            info.AddValue(nameof(this.Headers), this.Headers, typeof(Dictionary<string, string>));
            info.AddValue(nameof(this.StatusCode), this.StatusCode, typeof(HttpStatusCode?));
            info.AddValue(nameof(this.ReasonPhrase), this.ReasonPhrase, typeof(string));
            info.AddValue(nameof(this.RequestUri), this.RequestUri, typeof(string));

            base.GetObjectData(info, context);
        }

        private static string? GetDescription(PSObject? responseObj)
        {
            return responseObj?.Properties[nameof(Description)]?.Value as string;
        }
        private static string GetMessage(HttpRequestMessage request, Exception? inner, PSObject? responseObj, out string? requestUri)
        {
            requestUri = GetRequestUri(request);
            if (responseObj is null || !responseObj.TryGetNonNullProperty(nameof(Message), out string? msg))
            {
                return inner is null ?
                    $"An exception occurred in response -> {requestUri}"
                    : inner.GetBaseException().Message;
            }
            else
            {
                return msg;
            }
        }
        private static string GetMessage(HttpRequestMessage request, Exception? inner, SonarrServerError? serverError, out string? requestUri)
        {
            if (serverError is null)
            {
                return GetMessage(request, inner, (PSObject?)null, out requestUri);
            }

            requestUri = GetRequestUri(request);
            return serverError.Message;
        }
        private static string? GetRequestUri(HttpRequestMessage request)
        {
            return request.RequestUri?.ToString();
        }
        private static IReadOnlyDictionary<string, string> ParseResponseHeaders(HttpResponseMessage? response)
        {
            if (response is null)
            {
                return EmptyNameDictionary.Default;
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