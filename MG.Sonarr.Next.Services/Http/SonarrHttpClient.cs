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
        Task<PSObject?> SendGet(string path, CancellationToken token = default);
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

        public async Task<PSObject?> SendGet(string path, CancellationToken token = default)
        {
            using var response = await this.Client.GetAsync(path, token).ConfigureAwait(false);
            return await response.Content.ReadFromJsonAsync<PSObject>(this.Options, token);
        }
    }

    public static class SonarrClientDependencyInjection
    {
        static readonly ProductInfoHeaderValue _userAgent = new("PoshSonarr-Next", "2.0.0");

        public static IServiceCollection AddSonarrClient(this IServiceCollection services, IConnectionSettings settings)
        {
            services.AddSingleton(settings)
                    .AddTransient<OhHandler>()
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
                    .AddHttpMessageHandler<OhHandler>();

            return services;
        }
    }
}