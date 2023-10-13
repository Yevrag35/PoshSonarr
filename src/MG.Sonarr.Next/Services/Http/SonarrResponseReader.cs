using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models.Errors;
using Microsoft.Extensions.DependencyInjection;
using OneOf;
using System.Management.Automation;
using System.Net;
using System.Net.Http.Json;

namespace MG.Sonarr.Next.Services.Http
{
    public interface IResponseReader
    {
        Task<SonarrResponse> ReadNoResultAsync(HttpCall call, object? targetObj = null, CancellationToken token = default);
        Task<SonarrResponse<T>> ReadResultAsync<T>(HttpCall call, object? targetObj = null, CancellationToken token = default);
    }

    file sealed class SonarrResponseReader : IResponseReader
    {
        readonly JsonSerializerOptions _options;

        public SonarrResponseReader(ISonarrJsonOptions options)
        {
            _options = options.GetForDeserializing();
        }

        public async Task<SonarrResponse> ReadNoResultAsync(HttpCall call, object? targetObj = null, CancellationToken token = default)
        {
            if (call.IsEmpty)
            {
                throw new ArgumentException("HttpCall must be defined.");
            }
            else if (TryGetInvalidResult(in call, call.Response, out SonarrResponse result))
            {
                return result;
            }

            if (IsSuccessCode(call.Response.StatusCode, call.Method, out bool isIgnorable))
            {
                return SonarrResponse.Create(call.Response, call.RequestUri);
            }

            string? content = await call.Response.Content.ReadAsStringAsync(token);
            IErrorCollection deserializedError = GetErrorFromContent(content, _options);
            SonarrHttpException httpEx = new(call.Request, call.Response, deserializedError, null);
            SonarrErrorRecord record = new(httpEx, targetObj);

            return SonarrResponse.FromException(record);
        }
        public async Task<SonarrResponse<T>> ReadResultAsync<T>(HttpCall call, object? targetObj = null, CancellationToken token = default)
        {
            if (call.IsEmpty)
            {
                throw new ArgumentException("HttpCall must be defined.");
            }
            else if (TryGetInvalidResult(in call, call.Response, out SonarrResponse<T> result))
            {
                return result;
            }

            if (IsSuccessCode(call.Response.StatusCode, call.Method, out bool isIgnorable))
            {
                var oneOf = await this.ReadContentAsync<T>(call.Response, targetObj, token);

                return oneOf.TryPickT0(out SonarrErrorRecord? error, out T? remainder)
                    ? SonarrResponse.FromException<T>(error)
                    : new SonarrResponse<T>(call.RequestUri, remainder, null, call.Response.StatusCode);
            }

            string? content = await call.Response.Content.ReadAsStringAsync(token);
            IErrorCollection deserializedError = GetErrorFromContent(content, _options);
            SonarrHttpException httpEx = new(call.Request, call.Response, deserializedError, null);
            SonarrErrorRecord record = new(httpEx, targetObj);

            return SonarrResponse.FromException<T>(record);
        }

        private static IErrorCollection GetErrorFromContent(string? content, JsonSerializerOptions? options)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return ErrorCollection.Empty;
            }

            IErrorCollection list;
            try
            {
                if (content.StartsWith('{'))
                {
                    var error = JsonSerializer.Deserialize<SonarrServerError>(content, options);
                    list = error is not null
                        ? ErrorCollection.FromOne(error)
                        : ErrorCollection.Empty;
                }
                else if (content.StartsWith('['))
                {
                    list = JsonSerializer.Deserialize<ErrorCollection>(content, options)
                        ?? ErrorCollection.Empty;
                }
                else
                {
                    list = ErrorCollection.Empty;
                }
            }
            catch
            {
                list = ErrorCollection.Empty;
            }

            return list;
        }

        private async Task<OneOf<SonarrErrorRecord, T?>> ReadContentAsync<T>(HttpResponseMessage response, object? targetObj, CancellationToken token)
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<T>(_options, token);
            }
            catch (Exception e)
            {
                SonarrErrorRecord record = new(e, e.GetTypeName(), ErrorCategory.ParserError, targetObj);
                return record;
            }
        }

        private static bool TryGetInvalidResult(in HttpCall call, [NotNullWhen(false)] HttpResponseMessage? msg, out SonarrResponse result)
        {
            result = default;

            if (!call.HasResponse)
            {
                var ex = new EmptyHttpResponseException(call.RequestUri);
                result = SonarrResponse.FromException(ex.Url, ex, ErrorCategory.InvalidResult, HttpStatusCode.Unused);
                return true;
            }

            return !ReferenceEquals(call.Response, msg);
        }
        private static bool TryGetInvalidResult<T>(in HttpCall call, [NotNullWhen(false)] HttpResponseMessage? msg, out SonarrResponse<T> result)
        {
            result = default;

            if (!call.HasResponse)
            {
                var ex = new EmptyHttpResponseException(call.RequestUri);
                result = SonarrResponse.FromException<T>(ex.Url, ex, ErrorCategory.InvalidResult, HttpStatusCode.Unused);
                return true;
            }

            return !ReferenceEquals(call.Response, msg);
        }
        private static bool IsSuccessCode(HttpStatusCode? code, HttpMethod method, out bool isIgnorable)
        {
            isIgnorable = false;
            int? status = (int?)code;
            switch (status)
            {
                case <= 299:
                    return true;

                case 404:
                    isIgnorable = method == HttpMethod.Get;
                    return false;

                default:
                    return false;
            }
        }
    }

    internal static class SonarrResponseReaderDependencyInjection
    {
        public static IServiceCollection AddResponseReader(this IServiceCollection services)
        {
            return services.AddSingleton<IResponseReader, SonarrResponseReader>();
        }
    }
}
