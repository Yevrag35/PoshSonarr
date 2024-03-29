﻿using MG.Http.Urls.Queries;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Collections.Pools;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Services.Http.Handlers;
using MG.Sonarr.Next.Services.Http.Requests;
using Microsoft.Extensions.DependencyInjection;
using System.Management.Automation;
using System.Net.Http.Json;

namespace MG.Sonarr.Next.Services.Http.Clients
{
    public interface ISignalRClient : IDisposable
    {
        SonarrResponse<PingResponse> SendPing(CancellationToken token = default);
    }

    file sealed class SignalRClient : ISignalRClient
    {
        const long MIN_RNG = 100_000_000L;
        const string SIGNAL_R = "/signalr";
        const string PING = SIGNAL_R + "/ping";
        const string UNDERSCORE = "_";

        bool _disposed;
        readonly HttpClient _client;
        QueryParameterCollection _queryParams;
        readonly JsonSerializerOptions? _options;
        readonly Random _rng;   // this is *NOT* meant to be cryptographically secure.
        readonly IServiceScopeFactory _scopeFactory;

        public SignalRClient(HttpClient client, Random random, IConnectionSettings settings, ISonarrJsonOptions options, IObjectPool<QueryParameterCollection> pool, IServiceScopeFactory scopeFactory)
        {
            _client = client;
            _options = options.ForDeserializing;
            _queryParams = pool.Get();
            _queryParams.Add(nameof(IConnectionSettings.ApiKey), settings.ApiKey.GetValue());

            _rng = random;
            _scopeFactory = scopeFactory;
        }

        public SonarrResponse<PingResponse> SendPing(CancellationToken token = default)
        {
            string path = GetPingUrl(_queryParams, _rng);
            using PingRequestMessage request = new(path, _scopeFactory);

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
                response = _client.Send(request, token);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException httpEx)
            {
                SonarrHttpException sonarrEx = new(request, response, ErrorCollection.Empty, httpEx);
                return SonarrResponse.FromException<T>(
                    request.OriginalRequestUri, sonarrEx, ErrorCategory.ConnectionError, response?.StatusCode ?? System.Net.HttpStatusCode.Unused, response);
            }

            bool passed = TryDeserialize(response, _options, out T? result, out SonarrErrorRecord? error, token);
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

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var returner = scope.ServiceProvider.GetRequiredService<IObjectPool<QueryParameterCollection>>();
                    returner.Return(_queryParams);
                }

                _queryParams = null!;
                _disposed = true;
            }
        }
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
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
