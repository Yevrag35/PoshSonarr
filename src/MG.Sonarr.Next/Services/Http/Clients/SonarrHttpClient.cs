﻿using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerShell.Commands;
using System.Management.Automation;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Services.Http.Handlers;
using MG.Sonarr.Next.Services.Http.Requests;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Models.Errors;
using System.Reflection;

namespace MG.Sonarr.Next.Services.Http.Clients
{
    /// <summary>
    /// An interface exposing HTTP methods for issuing RESTful requests to Sonarr's API v3 endpoints.
    /// </summary>
    public interface ISonarrClient
    {
        SonarrResponse SendDelete(string path, CancellationToken token = default);
        Task<SonarrResponse> SendDeleteAsync(string path, CancellationToken token = default);
        SonarrResponse<T> SendGet<T>(string path, CancellationToken token = default);
        Task<SonarrResponse<T>> SendGetAsync<T>(string path, CancellationToken token = default);
        SonarrResponse<TOutput> SendPost<TOutput>(string path, CancellationToken token = default);
        SonarrResponse SendPost<T>(string path, T body, CancellationToken token = default) where T : notnull;
        Task<SonarrResponse> SendPostAsync<T>(string path, T body, CancellationToken token = default) where T : notnull;
        SonarrResponse<TOutput> SendPost<TBody, TOutput>(string path, TBody body, CancellationToken token = default) where TBody : notnull;
        Task<SonarrResponse<TOutput>> SendPostAsync<TBody, TOutput>(string path, TBody body, CancellationToken token = default) where TBody : notnull;
        SonarrResponse SendPut<T>(string path, T body, CancellationToken token = default)
            where T : notnull;
        Task<SonarrResponse> SendPutAsync<T>(string path, T body, CancellationToken token = default) where T : notnull;
        SonarrResponse SendTest(CancellationToken token = default);
    }

    internal sealed partial class SonarrHttpClient : ISonarrClient
    {
        const string TEST_API = "/system/status";

        readonly HttpClient _client;
        ISonarrJsonOptions _options;
        readonly IMetadataResolver _resolver;
        readonly IResponseReader _responseReader;
        readonly IServiceScopeFactory _scopeFactory;
        readonly IConnectionSettings _settings;

        public SonarrHttpClient(HttpClient client, IConnectionSettings settings, ISonarrJsonOptions options, IMetadataResolver resolver, IResponseReader reader, IServiceScopeFactory scopeFactory)
        {
            _client = client;
            _options = options;
            _resolver = resolver;
            _responseReader = reader;
            _settings = settings;
            _scopeFactory = scopeFactory;
        }

        public SonarrResponse SendDelete(string path, CancellationToken token = default)
        {
            using ApiKeyRequestMessage request = new(HttpMethod.Delete, path, _scopeFactory);

            return this.SendNoResultRequest(request, path, token);
        }

        public SonarrResponse<T> SendGet<T>(string path, CancellationToken token = default)
        {
            using ApiKeyRequestMessage request = new(HttpMethod.Get, path, _scopeFactory);

            var response = this.SendResultRequest<T>(request, path, token);
            if (response.IsDataTaggable(out IJsonMetadataTaggable? taggable))
            {
                taggable.SetTag(_resolver);
            }

            if (response.IsDataSortable(out ISortable? sortable) && sortable.Count > 1)
            {
                sortable.Sort();
            }

            return response;
        }
        public SonarrResponse SendPost<T>(string path, T body, CancellationToken token = default) where T : notnull
        {
            using ApiKeyRequestMessage request = new(HttpMethod.Post, path, _scopeFactory);
            request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);

            return this.SendNoResultRequest(request, path, token);
        }
        public SonarrResponse<TOutput> SendPost<TOutput>(string path, CancellationToken token = default)
        {
            using ApiKeyRequestMessage request = new(HttpMethod.Post, path, _scopeFactory);

            return this.SendResultRequest<TOutput>(request, path, token);
        }
        public SonarrResponse<TOutput> SendPost<TBody, TOutput>(string path, TBody body, CancellationToken token = default) where TBody : notnull
        {
            using ApiKeyRequestMessage request = new(HttpMethod.Post, path, _scopeFactory);

            request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);

            var response = this.SendResultRequest<TOutput>(request, path, token);
            if (response.IsDataTaggable(out IJsonMetadataTaggable? taggable))
            {
                taggable.SetTag(_resolver);
            }

            return response;
        }
        public SonarrResponse SendPut<T>(string path, T body, CancellationToken token = default) where T : notnull
        {
            using ApiKeyRequestMessage request = new(HttpMethod.Put, path, _scopeFactory);
            request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);

            return this.SendNoResultRequest(request, path, token);
        }
        public SonarrResponse SendTest(CancellationToken token = default)
        {
            HttpResponseMessage response = null!;
            using TestRequestMessage request = new(TEST_API, _scopeFactory);

            try
            {

                response = _client.Send(request, token);
                if (response.IsSuccessStatusCode)
                {
                    SonarrStatus? status = TryParseForStatus(response, _options.ForDeserializing, token);
                    SonarrAuthType authType = SonarrAuthType.None;
                    _settings.AuthType = (status?.TryGetAuthType(out authType)).GetValueOrDefault() 
                        ? authType : SonarrAuthType.None;
                }

                return response.IsSuccessStatusCode
                    ? SonarrResponse.Create(response, TEST_API)
                    : ParseMessage(TEST_API, response, token);
            }
            catch (Exception e)
            {
                var result = SonarrResponse.FromException(TEST_API, e, ErrorCategory.ConnectionError, response?.StatusCode ?? HttpStatusCode.Unused, response);
                response?.Dispose();
                return result;
            }
        }

        private static SonarrStatus? TryParseForStatus(HttpResponseMessage response, JsonSerializerOptions? options, CancellationToken token)
        {
            try
            {
                return response.Content.ReadFromJsonAsync<SonarrStatus>(options, token)
                    .GetAwaiter().GetResult();
            } 
            catch
            {
                return null;
            }
            finally
            {
                response.Dispose();
            }
        }
        private static SonarrServerError? ParseResponseForError(HttpResponseMessage? response, JsonSerializerOptions? options, CancellationToken token)
        {
            try
            {
                return response?.Content.ReadFromJsonAsync<SonarrServerError>(options, token).GetAwaiter().GetResult();
            }
            catch
            {
                return null;
            }
            finally
            {
                response?.Dispose();
            }
        }

        private SonarrResponse SendNoResultRequest(HttpRequestMessage request, string path, CancellationToken token)
        {
            HttpResponseMessage? response = null;
            try
            {
                response = _client.Send(request, token);
                return _responseReader.ReadNoResultAsync((path, request, response), path, token)
                    .GetAwaiter().GetResult();
            }
            catch (HttpRequestException httpEx)
            {
                var pso = ParseResponseForError(response, _options.ForDeserializing, token);
                SonarrHttpException sonarrEx = new(request, response, ErrorCollection.FromOne(pso), httpEx);

                var result = SonarrResponse.FromException(path, sonarrEx, ErrorCategory.InvalidResult, response?.StatusCode ?? HttpStatusCode.Unused, response);

                return result;
            }
            catch (Exception ex)
            {
                var result = SonarrResponse.FromException(path, ex, ErrorCategory.ConnectionError, response?.StatusCode ?? HttpStatusCode.Unused, response);

                return result;
            }
            finally
            {
                response?.Dispose();
            }
        }

        private SonarrResponse<T> SendResultRequest<T>(HttpRequestMessage request, string path, CancellationToken token)
        {
            HttpResponseMessage? response = null;

            try
            {
                response = _client.Send(request, token);
                return _responseReader.ReadResultAsync<T>((path, request, response), path, token)
                    .GetAwaiter().GetResult();
            }
            catch (HttpRequestException httpEx)
            {
                var pso = ParseResponseForError(response, _options.ForDeserializing, token);
                SonarrHttpException sonarrEx = new(request, response, ErrorCollection.FromOne(pso), httpEx);

                var result = SonarrResponse.FromException<T>(path, sonarrEx, ErrorCategory.InvalidResult, response?.StatusCode ?? HttpStatusCode.Unused, response);

                return result;
            }
            catch (TaskCanceledException cancelled)
            {
                ErrorCategory cat = cancelled.InnerException is TimeoutException tout
                    ? ErrorCategory.OperationTimeout
                    : ErrorCategory.OperationStopped;

                return ReturnFromException<T>(path, response, cat, cancelled);
            }
            catch (Exception e)
            {
                return ReturnFromException<T>(path, response, ErrorCategory.ConnectionError, e);
            }
        }

        private static SonarrResponse<T> ReturnFromException<T>(string path, HttpResponseMessage? response, ErrorCategory category, Exception e)
        {
            var result = SonarrResponse.FromException<T>(path, e, category, response?.StatusCode ?? HttpStatusCode.Unused, response);
            response?.Dispose();
            return result;
        }

        private static SonarrResponse ParseMessage(string url, HttpResponseMessage response, CancellationToken token)
        {
            JsonNode? node = JsonNode.Parse(response.Content.ReadAsStream(token));
            return
                SonarrResponse.FromException(url, new HttpResponseException(node?.AsObject()["message"]?.ToJsonString(), response), ErrorCategory.ResourceUnavailable, response.StatusCode);
        }
    }

    public static class SonarrClientDependencyInjection
    {
        internal const string API_HEADER_KEY = "X-Api-Key";

        internal static readonly ProductInfoHeaderValue UserAgent = new("PoshSonarr-Next", "2.0.0");
        
        private static IHttpClientBuilder AddSonarrClientInternal(
            IServiceCollection services,
            Assembly cmdletAssembly,
            IConnectionSettings settings,
            Action<IServiceProvider, JsonSerializerOptions> configureJson)
        {
            return services
                    .AddMetadata(cmdletAssembly)
                    .AddResponseReader()
                    .AddSingleton(settings)
                    .AddSignalRClient()
                    .AddSonarrDownloadClient()
                    .AddSonarrJsonOptions(configureJson)
                    .AddTransient<SonarrClientHandler>()
                    .AddHttpClient<ISonarrClient, SonarrHttpClient>((provider, client) =>
                    {
                        var settings = provider.GetRequiredService<IConnectionSettings>();
                        client.BaseAddress = settings.ServiceUri;
                        client.Timeout = settings.Timeout;
                        client.DefaultRequestHeaders
                            .Add(API_HEADER_KEY, settings.ApiKey.GetValue());

                        client.DefaultRequestHeaders.UserAgent.Add(UserAgent);

                    })
                    .ConfigurePrimaryHttpMessageHandler<SonarrClientHandler>();
        }
        public static IServiceCollection AddSonarrNonPSClient(this IServiceCollection services,
            IConnectionSettings settings,
            Action<IServiceProvider, JsonSerializerOptions> configureJson)
        {
            services.AddTransient<PathHandler>()
                    .AddTransient<TestingHandler>();

            AddSonarrClientInternal(services, typeof(SonarrHttpClient).Assembly, settings, configureJson)
                .AddHttpMessageHandler<PathHandler>()
                .AddHttpMessageHandler<TestingHandler>();

            return services;
        }
        public static IServiceCollection AddSonarrClient(this IServiceCollection services,
            Assembly cmdletAssembly,
            IConnectionSettings settings,
            Action<IServiceProvider, JsonSerializerOptions> configureJson)
        {
            services.AddTransient<PathHandler>()
                    .AddTransient<VerboseHandler>()
                    .AddTransient<TestingHandler>();

            AddSonarrClientInternal(services, cmdletAssembly, settings, configureJson)
                .AddHttpMessageHandler<PathHandler>()
                .AddHttpMessageHandler<VerboseHandler>()
                .AddHttpMessageHandler<TestingHandler>();

            return services;
        }
    }
}