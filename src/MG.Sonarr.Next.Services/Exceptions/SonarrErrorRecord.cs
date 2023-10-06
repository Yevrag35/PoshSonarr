using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Services.Extensions;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Net;

namespace MG.Sonarr.Next.Services.Exceptions
{
    public sealed class SonarrErrorRecord : ErrorRecord
    {
        static readonly IReadOnlyDictionary<string, string> _empty =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

        readonly IReadOnlyDictionary<string, string> _headers;

        public IReadOnlyDictionary<string, string> Headers => _headers;
        public string Message => this.ErrorDetails?.Message ?? this.Exception.Message;
        public bool IsIgnorable { get; }
        public HttpStatusCode? StatusCode { get; }
        public string? ReasonPhrase { get; }
        public string? RequestUri { get; }

        public SonarrErrorRecord(SonarrHttpException exception, HttpResponseMessage? response, object? targetObj = null)
            : base(exception, exception.GetTypeName(), GetCategoryFromStatusCode(exception.StatusCode, out bool isIgnorable), targetObj)
        {
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
        public SonarrErrorRecord(SonarrHttpException response, object? targetObj = null)
            : this(exception: response, response: response.Response, targetObj)
        {
        }

        public SonarrErrorRecord(Exception normalEx, string errorId, ErrorCategory category, object? targetObj)
            : base(exception: normalEx, errorId, category, targetObj)
        {
            _headers = _empty;
        }

        public SonarrErrorRecord(ErrorRecord wraps)
            : base(wraps, wraps.Exception)
        {
            _headers = _empty;
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
