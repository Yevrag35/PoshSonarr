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
        Task<SonarrResponse<T>> SendGet<T>(string path, CancellationToken token = default);
    }

    file sealed class SonarrHttpClient : ISonarrClient
    {
        internal const string API_HEADER_KEY = "X-Api-Key";

        HttpClient Client { get; }
        JsonSerializerOptions Options { get; }

        public SonarrHttpClient(HttpClient client, SonarrJsonOptions options)
        {
            this.Client = client;
            this.Options = options.GetOptions();
        }

        public async Task<SonarrResponse<T>> SendGet<T>(string path, CancellationToken token = default)
        {
            HttpResponseMessage response;
            try
            {
                response = await this.Client.GetAsync(path, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                return SonarrResponse.FromException<T>(path, e, ErrorCategory.ConnectionError);
            }

            try
            {
                T? result = await response.Content.ReadFromJsonAsync<T>(this.Options, token);
                return response.ToResult(path, result);
            }
            catch (Exception e)
            {
                return SonarrResponse.FromException<T>(response.RequestMessage?.RequestUri?.OriginalString ?? path, e, ErrorCategory.ParserError);
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