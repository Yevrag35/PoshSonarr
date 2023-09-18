using MG.Sonarr.Next.Services.Exceptions;
using System.Management.Automation;
using System.Net;

namespace MG.Sonarr.Next.Services.Http
{
    public interface ISonarrResponse
    {
        ErrorRecord? Error { get; }
        [MemberNotNullWhen(true, nameof(Error))]
        bool IsError { get; }
        HttpStatusCode StatusCode { get; }
        string RequestUrl { get; }
    }

    public readonly struct SonarrResponse : ISonarrResponse
    {
        readonly ErrorRecord? _error;
        readonly bool _isError;
        readonly HttpStatusCode _statusCode;
        readonly string? _url;

        public ErrorRecord? Error => _error;
        [MemberNotNullWhen(true, nameof(_error), nameof(Error))]
        public bool IsError => _isError;
        public HttpStatusCode StatusCode => _statusCode;
        public string RequestUrl => _url ?? string.Empty;

        private SonarrResponse(string url, HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
            _error = null;
            _isError = false;
            _url = url ?? string.Empty;
        }
        private SonarrResponse(string url, ErrorRecord record, HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
            _error = record;
            _isError = true;
            _url = url ?? string.Empty;
        }

        public static SonarrResponse<T> FromException<T>(string url, Exception exception, ErrorCategory category, HttpStatusCode statusCode)
        {
            Type eType = exception.GetType();
            string name = eType.FullName ?? eType.Name;
            ErrorRecord record = new(exception, name, category, url);
            return new SonarrResponse<T>(url, default, record, statusCode);
        }
        public static SonarrResponse FromException(string url, Exception exception, ErrorCategory category, HttpStatusCode statusCode)
        {
            Type eType = exception.GetType();
            string name = eType.FullName ?? eType.Name;
            ErrorRecord record = new(exception, name, category, url);
            return new SonarrResponse(url, record, statusCode);
        }

        public static SonarrResponse Create(HttpResponseMessage message, string path)
        {
            return new SonarrResponse(message.RequestMessage?.RequestUri?.ToString() ?? path, message.StatusCode);
        }
    }

    public readonly struct SonarrResponse<T> : ISonarrResponse
    {
        readonly T? _data;
        readonly string? _url;
        readonly ErrorRecord? _error;
        readonly bool _isError;
        readonly bool _isNotEmpty;
        readonly HttpStatusCode _statusCode;

        public T? Data => _data;
        public ErrorRecord? Error => _error;
        public bool IsEmpty => !_isNotEmpty;
        [MemberNotNullWhen(true, nameof(Error))]
        [MemberNotNullWhen(false, nameof(Data))]
        public bool IsError => _isError;
        public string RequestUrl => _url ?? string.Empty;
        public HttpStatusCode StatusCode => _statusCode;

        public SonarrResponse(string? url, T? data, ErrorRecord? error, HttpStatusCode statusCode)
        {
            _data = data;
            _statusCode = statusCode;
            _url = url;
            _error = error;
            bool isError = error is not null;
            _isNotEmpty = isError || data is not null;
            _isError = isError;
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
