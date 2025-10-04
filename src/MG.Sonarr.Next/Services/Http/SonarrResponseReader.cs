using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models.Errors;
using MG.Sonarr.Next.Services.Http.Handlers;
using MG.Sonarr.Next.Unions;
using Microsoft.Extensions.DependencyInjection;
using System.Management.Automation;
using System.Net;
using System.Net.Http.Json;

namespace MG.Sonarr.Next.Services.Http
{
    public interface IResponseReader
    {
        Task<SonarrClientResult> ReadNoResultAsync(HttpCall call, object? targetObj = null, CancellationToken token = default);
        Task<SonarrClientResult<T>> ReadResultAsync<T>(HttpCall call, object? targetObj = null, CancellationToken token = default);
    }

    internal sealed class SonarrResponseReader : IResponseReader
    {
        readonly JsonSerializerOptions _options;

        public SonarrResponseReader(ISonarrJsonOptions options)
        {
            _options = options.ForDeserializing;
        }

        public async Task<SonarrClientResult> ReadNoResultAsync(HttpCall call, object? targetObj = null, CancellationToken token = default)
        {
            if (TryGetInvalidResult(call, call.Response, out SonarrClientResult? result))
            {
                return result;
            }

            if (IsSuccessCode(call.Response.StatusCode, call.Method, out bool isIgnorable))
            {
                return new(call.Response, call.RequestUri);
            }

            string? content = await call.Response.Content.ReadAsStringAsync(token);
            IErrorCollection deserializedError = GetErrorFromContent(content, _options);
            SonarrHttpException httpEx = new(call.Request, call.Response, deserializedError, null);
            SonarrErrorRecord record = new(httpEx, targetObj);

            return new(record);
        }
        public async Task<SonarrClientResult<T>> ReadResultAsync<T>(HttpCall call, object? targetObj = null, CancellationToken token = default)
        {
            if (((IReadOnlyDictionary<string, object?>)call.Request.Options).ContainsKey(ErrorHandler.Is404))
            {
                return SonarrClientResult.NotFound<T>();
            }

            if (TryGetInvalidResult(call, call.Response, out SonarrClientResult<T>? result))
            {
                return result;
            }

            if (IsSuccessCode(call.Response.StatusCode, call.Method, out bool isIgnorable))
            {
                var oneOf = await this.ReadContentAsync<T>(call.Response, targetObj, token);

                unsafe
                {
                    return oneOf.Match(call,
                        f1: &ReadErrorFromOneOf<T>,
                        f2: &ReadContentFromOneOf);
                }
            }

            string? content = await call.Response.Content.ReadAsStringAsync(token);
            IErrorCollection deserializedError = GetErrorFromContent(content, _options);
            SonarrHttpException httpEx = new(call.Request, call.Response, deserializedError, null);
            SonarrErrorRecord record = new(httpEx, targetObj);

            return new(record);
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
            catch (Exception e)
            {
                Debug.Fail(e.Message);
                list = ErrorCollection.Empty;
            }

            return list;
        }

        private async Task<Either<SonarrErrorRecord, T>> ReadContentAsync<T>(HttpResponseMessage response, object? targetObj, CancellationToken token)
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<T>(_options, token)
                    ?? throw new JsonException("Unable to deserialize the response content.");
            }
            catch (Exception e)
            {
                SonarrErrorRecord record = new(e, e.GetTypeName(), ErrorCategory.ParserError, targetObj);
                return record;
            }
        }

        private static bool TryGetInvalidResult(HttpCall call, [NotNullWhen(false)] HttpResponseMessage? msg, [NotNullWhen(true)] out SonarrClientResult? result)
        {
            result = null;
            if (!call.HasResponse)
            {
                var ex = new EmptyHttpResponseException(call.RequestUri);
                result = SonarrClientResult.FromException(ex, ErrorCategory.InvalidResult, ErrorHandler.NoResponseCode, msg);
                return true;
            }

            return !ReferenceEquals(call.Response, msg);
        }
        private static bool TryGetInvalidResult<T>(HttpCall call, [NotNullWhen(false)] HttpResponseMessage? msg, [NotNullWhen(true)] out SonarrClientResult<T>? result)
        {
            result = null;

            if (!call.HasResponse)
            {
                EmptyHttpResponseException ex = new(call.RequestUri);
                result = SonarrClientResult.FromException<T>(ex, ErrorCategory.InvalidResult, ErrorHandler.NoResponseCode, msg);
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
                case <= 399:
                    return true;

                case 404:
                    isIgnorable = method == HttpMethod.Get;
                    return false;

                default:
                    return false;
            }
        }

        private static SonarrClientResult<T> ReadErrorFromOneOf<T>(SonarrErrorRecord error, HttpCall call)
        {
            return new(error)
            {
                StatusCode = error.StatusCode ?? call.Response!.StatusCode,
            };
        }
        private static SonarrClientResult<T> ReadContentFromOneOf<T>(T content, HttpCall call)
        {
            return SonarrClientResult.Create(content, call.Response!, call.RequestUri);
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
