using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerShell.Commands;
using System.Management.Automation;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using MG.Sonarr.Next.Services.Models;

namespace MG.Sonarr.Next.Services.Http
{
    public interface ISonarrClient
    {
        SonarrResponse SendDelete(string path, CancellationToken token = default);
        SonarrResponse<T> SendGet<T>(string path, CancellationToken token = default);
        SonarrResponse SendPost<T>(string path, T body, CancellationToken token = default) where T : notnull;
        SonarrResponse<TOutput> SendPost<TBody, TOutput>(string path, TBody body, CancellationToken token = default) where TBody : notnull;
        SonarrResponse SendPut<T>(string path, T body, CancellationToken token = default)
            where T : notnull;
        SonarrResponse SendTest(CancellationToken token = default);
    }

    file sealed class SonarrHttpClient : ISonarrClient
    {
        internal const string API_HEADER_KEY = "X-Api-Key";
        const string TEST_API = "/system/status";

        HttpClient Client { get; }
        JsonSerializerOptions DeserializingOptions { get; }
        MetadataResolver Resolver { get; }
        IResponseReader ResponseReader { get; }
        JsonSerializerOptions SerializingOptions { get; }

        public SonarrHttpClient(HttpClient client, SonarrJsonOptions options, MetadataResolver resolver, IResponseReader reader)
        {
            this.Client = client;
            this.DeserializingOptions = options.GetForDeserializing();
            this.Resolver = resolver;
            this.ResponseReader = reader;
            this.SerializingOptions = options.GetForSerializing();
        }

        public SonarrResponse SendDelete(string path, CancellationToken token = default)
        {
            using HttpRequestMessage request = new(HttpMethod.Delete, path);
            request.Options.Set(SonarrClientDependencyInjection.KEY, false);

            return this.SendNoResultRequest(request, path, token);
        }

        public SonarrResponse<T> SendGet<T>(string path, CancellationToken token = default)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, path);
            request.Options.Set(SonarrClientDependencyInjection.KEY, false);

            var response = this.SendResultRequest<T>(request, path, token);
            if (response.IsDataTaggable(out IJsonMetadataTaggable? taggable))
            {
                taggable.SetTag(this.Resolver);
            }

            return response;
        }
        public SonarrResponse SendPost<T>(string path, T body, CancellationToken token = default) where T : notnull
        {
            using HttpRequestMessage request = new(HttpMethod.Post, path);
            request.Options.Set(SonarrClientDependencyInjection.KEY, false);
            request.Content = JsonContent.Create(body, body.GetType(), options: this.SerializingOptions);

            return this.SendNoResultRequest(request, path, token);
        }
        public SonarrResponse<TOutput> SendPost<TBody, TOutput>(string path, TBody body, CancellationToken token = default) where TBody : notnull
        {
            using HttpRequestMessage request = new(HttpMethod.Post, path);
            request.Options.Set(SonarrClientDependencyInjection.KEY, false);

            request.Content = JsonContent.Create(body, body.GetType(), options: this.SerializingOptions);

            var response = this.SendResultRequest<TOutput>(request, path, token);
            if (response.IsDataTaggable(out IJsonMetadataTaggable? taggable))
            {
                taggable.SetTag(this.Resolver);
            }

            return response;
        }
        public SonarrResponse SendPut<T>(string path, T body, CancellationToken token = default)
            where T : notnull
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Options.Set(SonarrClientDependencyInjection.KEY, false);
            request.Content = JsonContent.Create(body, body!.GetType(), options: this.SerializingOptions);

            return this.SendNoResultRequest(request, path, token);
        }
        public SonarrResponse SendTest(CancellationToken token = default)
        {
            HttpResponseMessage response = null!;
            using HttpRequestMessage request = new(HttpMethod.Get, TEST_API);
            request.Options.Set(SonarrClientDependencyInjection.KEY, true);

            try
            {
                
                response = this.Client.Send(request, token);
                return response.IsSuccessStatusCode
                    ? SonarrResponse.Create(response, TEST_API)
                    : ParseMessage(TEST_API, response, token);
            }
            catch (Exception e)
            {
                var result = SonarrResponse.FromException(TEST_API, e, ErrorCategory.ConnectionError, response?.StatusCode ?? System.Net.HttpStatusCode.Unused, response);
                response?.Dispose();
                return result;
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
                response = this.Client.Send(request, token);
                return this.ResponseReader.ReadNoResult((path, request, response), path, token)
                    .GetAwaiter().GetResult();
            }
            catch (HttpRequestException httpEx)
            {
                var pso = ParseResponseForError(response, this.DeserializingOptions, token);
                SonarrHttpException sonarrEx = new(request, response, pso, httpEx);

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
                response = this.Client.Send(request, token);
                return this.ResponseReader.ReadResult<T>((path, request, response), path, token)
                    .GetAwaiter().GetResult();
            }
            catch (HttpRequestException httpEx)
            {
                var pso = ParseResponseForError(response, this.DeserializingOptions, token);
                SonarrHttpException sonarrEx = new(request, response, pso, httpEx);

                var result = SonarrResponse.FromException<T>(path, sonarrEx, ErrorCategory.InvalidResult, response?.StatusCode ?? HttpStatusCode.Unused, response);

                return result;
            }
            catch (Exception e)
            {
                var result = SonarrResponse.FromException<T>(path, e, ErrorCategory.ConnectionError, response?.StatusCode ?? HttpStatusCode.Unused, response);
                response?.Dispose();
                return result;
            }
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
        internal static readonly HttpRequestOptionsKey<bool> KEY = new(nameof(ISonarrClient.SendTest));
        static readonly ProductInfoHeaderValue _userAgent = new("PoshSonarr-Next", "2.0.0");

        public static IServiceCollection AddSonarrClient(this IServiceCollection services, IConnectionSettings settings)
        {
            services.AddSingleton(settings)
                    .AddResponseReader()
                    .AddTransient<PathHandler>()
                    .AddTransient<VerboseHandler>()
                    .AddTransient<TestingHandler>()
                    .AddTransient<SonarrClientHandler>()
                    .AddHttpClient<ISonarrClient, SonarrHttpClient>((provider, client) =>
                    {
                        var settings = provider.GetRequiredService<IConnectionSettings>();
                        client.BaseAddress = settings.ServiceUri;
                        client.Timeout = settings.Timeout;
                        client.DefaultRequestHeaders
                            .Add(SonarrHttpClient.API_HEADER_KEY, settings.ApiKey.GetValue());

                        client.DefaultRequestHeaders.UserAgent.Add(_userAgent);

                    })
                    .ConfigurePrimaryHttpMessageHandler<SonarrClientHandler>()
                    .AddHttpMessageHandler<PathHandler>()
                    .AddHttpMessageHandler<VerboseHandler>()
                    .AddHttpMessageHandler<TestingHandler>();

            return services;
        }
    }
}