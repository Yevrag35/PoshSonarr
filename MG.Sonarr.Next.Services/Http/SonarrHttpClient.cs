using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerShell.Commands;
using System.Collections;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace MG.Sonarr.Next.Services.Http
{
    public interface ISonarrClient
    {
        SonarrResponse<T> SendGet<T>(string path, CancellationToken token = default);
        SonarrResponse SendPut<T>(string path, T body, CancellationToken token = default);
        SonarrResponse SendTest(CancellationToken token = default);
    }

    file sealed class SonarrHttpClient : ISonarrClient
    {
        internal const string API_HEADER_KEY = "X-Api-Key";
        const string TEST_API = "/system/status";

        HttpClient Client { get; }
        JsonSerializerOptions DeserializingOptions { get; }
        JsonSerializerOptions SerializingOptions { get; }

        public SonarrHttpClient(HttpClient client, SonarrJsonOptions options)
        {
            this.Client = client;
            this.DeserializingOptions = options.GetForDeserializing();
            this.SerializingOptions = options.GetForSerializing();
        }

        public SonarrResponse<T> SendGet<T>(string path, CancellationToken token = default)
        {
            HttpResponseMessage response = null!;
            using HttpRequestMessage request = new(HttpMethod.Get, path);
            request.Options.Set(SonarrClientDependencyInjection.KEY, false);

            try
            {
                response = this.Client.Send(request, token);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                var result = SonarrResponse.FromException<T>(path, e, ErrorCategory.ConnectionError, response?.StatusCode ?? System.Net.HttpStatusCode.Unused);
                response?.Dispose();
                return result;
            }

            using (response)
            {
                try
                {
                    T? result = response.Content.ReadFromJsonAsync<T>(this.DeserializingOptions, token).GetAwaiter().GetResult();

                    return response.ToResult(path, result);
                }
                catch (Exception e)
                {
                    return SonarrResponse.FromException<T>(response.RequestMessage?.RequestUri?.OriginalString ?? path, e, ErrorCategory.ParserError, response.StatusCode);
                }
            }
        }

        public SonarrResponse SendPut<T>(string path, T body, CancellationToken token = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Options.Set(SonarrClientDependencyInjection.KEY, false);
            request.Content = JsonContent.Create(body, options: this.SerializingOptions);

            HttpResponseMessage? response = null;
            try
            {
                response = this.Client.Send(request, token);
                response.EnsureSuccessStatusCode();
                return SonarrResponse.Create(response, path);
            }
            catch (Exception e)
            {
                return SonarrResponse.FromException(request.RequestUri?.ToString() ?? path,
                    e, ErrorCategory.InvalidResult, response?.StatusCode ?? System.Net.HttpStatusCode.Unused);
            }
            finally
            {
                response?.Dispose();
            }
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
                var result = SonarrResponse.FromException(TEST_API, e, ErrorCategory.ConnectionError, response?.StatusCode ?? System.Net.HttpStatusCode.Unused);
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

        public static IServiceCollection AddSonarrClient(this IServiceCollection services, IConnectionSettings settings, Action<HttpResponseMessage>? callback = null)
        {
            services.AddSingleton(settings)
                    .AddTransient<PathHandler>()
                    .AddTransient<VerboseHandler>()
                    .AddTransient<SonarrClientHandler>()
                    .AddSingleton(callback ?? ((HttpResponseMessage x) => { }))
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
                    .AddHttpMessageHandler<VerboseHandler>()
                    .AddHttpMessageHandler<PathHandler>();

            return services;
        }
    }
}