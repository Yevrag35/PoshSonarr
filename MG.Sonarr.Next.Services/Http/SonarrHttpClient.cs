using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Management.Automation;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MG.Sonarr.Next.Services.Http
{
    public interface ISonarrClient
    {
        Task<SonarrResponse<T>> SendGetAsync<T>(string path, CancellationToken token = default);
        Task<SonarrResponse> SendPutAsync(string path, PSObject body, CancellationToken token = default);
    }

    file sealed class SonarrHttpClient : ISonarrClient
    {
        internal const string API_HEADER_KEY = "X-Api-Key";

        HttpClient Client { get; }
        JsonSerializerOptions DeserializingOptions { get; }
        JsonSerializerOptions SerializingOptions { get; }

        public SonarrHttpClient(HttpClient client, SonarrJsonOptions options)
        {
            this.Client = client;
            this.DeserializingOptions = options.GetForDeserializing();
            this.SerializingOptions = options.GetForSerializing();
        }

        public async Task<SonarrResponse<T>> SendGetAsync<T>(string path, CancellationToken token = default)
        {
            HttpResponseMessage response = null!;
            try
            {
                response = await this.Client.GetAsync(path, token).ConfigureAwait(false);
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
                    T? result = await response.Content.ReadFromJsonAsync<T>(this.DeserializingOptions, token);
                    return response.ToResult(path, result);
                }
                catch (Exception e)
                {
                    return SonarrResponse.FromException<T>(response.RequestMessage?.RequestUri?.OriginalString ?? path, e, ErrorCategory.ParserError, response.StatusCode);
                }
            }
        }
        public async Task<SonarrResponse> SendPutAsync(string path, PSObject body, CancellationToken token = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, path);
            request.Content = JsonContent.Create(body, options: this.SerializingOptions);

            HttpResponseMessage? response = null;
            try
            {
                response = await this.Client.SendAsync(request, token).ConfigureAwait(false);
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
    }

    public static class SonarrClientDependencyInjection
    {
        static readonly ProductInfoHeaderValue _userAgent = new("PoshSonarr-Next", "2.0.0");

        public static IServiceCollection AddSonarrClient(this IServiceCollection services, IConnectionSettings settings)
        {
            services.AddSingleton(settings)
                    .AddTransient<PathHandler>()
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
                    .AddHttpMessageHandler<PathHandler>();

            return services;
        }
    }
}