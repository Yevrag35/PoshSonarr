using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Net;

namespace MG.Sonarr.Next.Exceptions
{
    /// <summary>
    /// Represents an error thrown from the PoshSonarr PowerShell module.
    /// </summary>
    /// <inheritdoc cref="ErrorRecord"/>
    public sealed class SonarrErrorRecord : ErrorRecord
    {
        readonly IReadOnlyDictionary<string, string> _headers;

        /// <inheritdoc cref="SonarrHttpException.Headers"/>
        public IReadOnlyDictionary<string, string> Headers => _headers;

        /// <inheritdoc cref="ErrorDetails.Message"/>
        public string Message => this.ErrorDetails?.Message ?? this.Exception.Message;
        /// <summary>
        /// Indicates whether this error record can be written through <see cref="Cmdlet.WriteError(ErrorRecord)"/> conditionally. If <see langword="true"/>, this record should only be optionally
        /// written to the error stream.
        /// </summary>
        public bool IsIgnorable { get; }

        /// <inheritdoc cref="SonarrHttpException.StatusCode"/>
        public HttpStatusCode? StatusCode { get; }

        /// <inheritdoc cref="SonarrHttpException.ReasonPhrase"/>
        public string? ReasonPhrase { get; }

        /// <inheritdoc cref="SonarrHttpException.RequestUri"/>
        public string? RequestUri { get; }

        public SonarrErrorRecord(SonarrHttpException exception, HttpResponseMessage? response)
            : this(exception, response, (object?)null)
        {
        }
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
            this.CategoryInfo.TargetType = targetObj?.GetType().GetTypeName();
        }
        public SonarrErrorRecord(SonarrHttpException response)
            : this(exception: response, response: response?.Response, (object?)null)
        {
        }
        public SonarrErrorRecord(SonarrHttpException response, object? targetObj)
            : this(exception: response, response: response?.Response, targetObj)
        {
        }

        public SonarrErrorRecord(Exception normalEx, string errorId, ErrorCategory category, object? targetObj)
            : base(exception: normalEx, errorId, category, targetObj)
        {
            _headers = EmptyNameDictionary.Default;
        }

        public SonarrErrorRecord(ErrorRecord wraps)
            : base(wraps, wraps?.Exception)
        {
            _headers = EmptyNameDictionary.Default;
        }

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
}
