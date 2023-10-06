using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Collections;
using MG.Sonarr.Next.Services.Exceptions;
using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http.Handlers;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Services.Http.Requests;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Models;
using MG.Sonarr.Next.Services.Models.System;
using Microsoft.Extensions.DependencyInjection;
using System.Management.Automation;
using System.Net.Http.Json;

namespace MG.Sonarr.Next.Services.Http.Clients
{
    public interface ISignalRClient
    {
        SonarrResponse<PingResponse> SendPing(CancellationToken token = default);
    }

    file sealed class SignalRClient : ISignalRClient
    {
        const long MIN_RNG = 100_000_000L;
        const string SIGNAL_R = "/signalr";
        const string PING = SIGNAL_R + "/ping";
        const string UNDERSCORE = "_";
        readonly Random _rng;   // this is *NOT* meant to be cryptographically secure.

        HttpClient Client { get; }
        QueryParameterCollection QueryParams { get; }
        JsonSerializerOptions? Options { get; }

        public SignalRClient(HttpClient client, Random random, IConnectionSettings settings, SonarrJsonOptions options)
        {
            this.Client = client;
            this.Options = options.GetForDeserializing();
            this.QueryParams = new(2)
            {
                { nameof(IConnectionSettings.ApiKey), settings.ApiKey.GetValue() }
            };

            _rng = random;
        }

        public SonarrResponse<PingResponse> SendPing(CancellationToken token = default)
        {
            string path = GetPingUrl(this.QueryParams, _rng);
            using PingRequestMessage request = new(path);

            return this.SendRequest<PingResponse>(request, token);
        }

        private static IQueryParameter GetRandomNumberQuery(Random rng)
        {
            long rand = rng.NextInt64(MIN_RNG, long.MaxValue);
            return QueryParameter.Create(UNDERSCORE, rand, LengthConstants.LONG_MAX);
        }
        private static string GetPingUrl(QueryParameterCollection queryParams, Random rng)
        {
            IQueryParameter randParam = GetRandomNumberQuery(rng);
            queryParams.AddOrUpdate(randParam);

            Span<char> span = stackalloc char[PING.Length + 1 + queryParams.MaxLength];
            
            int position = 0;
            PING.CopyToSlice(span, ref position);
            
            span[position++] = '?';

            _ = queryParams.TryFormat(span.Slice(position), out int written, default, Statics.DefaultProvider);

            return new string(span.Slice(0, position + written));
        }

        private SonarrResponse<T> SendRequest<T>(SonarrRequestMessage request, CancellationToken token)
        {
            HttpResponseMessage response = null!;
            try
            {
                response = this.Client.Send(request, token);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException httpEx)
            {
                SonarrHttpException sonarrEx = new(request, response, ErrorCollection.Empty, httpEx);
                return SonarrResponse.FromException<T>(
                    request.OriginalRequestUri, sonarrEx, ErrorCategory.ConnectionError, response?.StatusCode ?? System.Net.HttpStatusCode.Unused, response);
            }

            bool passed = TryDeserialize(response, this.Options, out T? result, out SonarrErrorRecord? error, token);
            Debug.Assert(passed);

            return new SonarrResponse<T>(request.OriginalRequestUri, result, error, response.StatusCode);
        }

        private static bool TryDeserialize<T>(HttpResponseMessage response, JsonSerializerOptions? options, [NotNullWhen(true)] out T? result, [NotNullWhen(false)] out SonarrErrorRecord? error, CancellationToken token)
        {
            error = null;

            try
            {
                result = response.Content.ReadFromJsonAsync<T>(options, token)
                    .GetAwaiter().GetResult();

                if (result is null)
                {
                    EmptyHttpResponseException ex = new(response.RequestMessage?.RequestUri?.ToString() ??string.Empty);
                    error = new SonarrErrorRecord(ex, ex.GetTypeName(), ErrorCategory.InvalidResult, response);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                result = default;
                error = new SonarrErrorRecord(ex, ex.GetTypeName(), ErrorCategory.ParserError, response);
                return false;
            }
        }
    }

    internal static class SignalRDepedendencyInjection
    {
        internal static IServiceCollection AddSignalRClient(this IServiceCollection services)
        {
            services.AddSingleton<Random>()
                    .AddHttpClient<ISignalRClient, SignalRClient>((provider, client) =>
                    {
                        var settings = provider.GetRequiredService<IConnectionSettings>();
                        client.BaseAddress = settings.ServiceUri;
                        client.DefaultRequestHeaders
                            .UserAgent.Add(SonarrClientDependencyInjection.UserAgent);
                    })
                    .ConfigurePrimaryHttpMessageHandler<SonarrClientHandler>()
                    .AddHttpMessageHandler<VerboseHandler>();

            return services;
        }
    }
}
