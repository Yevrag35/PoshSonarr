using MG.Sonarr.Next.Services.Exceptions;
using System.Management.Automation;

namespace MG.Sonarr.Next.Services.Http
{
    public static class SonarrResponse
    {
        public static SonarrResponse<T> FromException<T>(string url, Exception exception, ErrorCategory category)
        {
            Type eType = exception.GetType();
            string name = eType.FullName ?? eType.Name;
            ErrorRecord record = new(exception, name, category, url);
            return new SonarrResponse<T>(url, default, record);
        }
    }

    public readonly struct SonarrResponse<T>
    {
        readonly T? _data;
        readonly string? _url;
        readonly ErrorRecord? _error;
        readonly bool _isError;
        readonly bool _isNotEmpty;

        public T? Data => _data;
        public ErrorRecord? Error => _error;
        public bool IsEmpty => !_isNotEmpty;
        [MemberNotNullWhen(true, nameof(Error))]
        [MemberNotNullWhen(false, nameof(Data))]
        public bool IsError => _isError;
        public string RequestUrl => _url ?? string.Empty;

        public SonarrResponse(string? url, T? data, ErrorRecord? error)
        {
            _data = data;
            _url = url;
            _error = error;
            bool isError = error is not null;
            _isNotEmpty = isError || data is not null;
            _isError = isError;
        }

        public static implicit operator SonarrResponse<T>(ValueTuple<string?, T> success)
        {
            return new(success.Item1, success.Item2, null);
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
                    url, new EmptyHttpResponseException(url), ErrorCategory.InvalidResult);
            }

            return new SonarrResponse<T>(url, data, null);
        }
    }
}
