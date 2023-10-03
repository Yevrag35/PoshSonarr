﻿using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Http.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System.Management.Automation;
using System.Net;

namespace MG.Sonarr.Next.Services.Http.Clients
{
    public interface ISonarrDownloadClient
    {
        SonarrResponse<string> DownloadToPath(string url, string path, CancellationToken token = default);
        SonarrResponse<string> DownloadToPath(string url, string path, NetworkCredential? credential, CancellationToken token = default);
    }

    file sealed class SonarrDownloadClient : ISonarrDownloadClient
    {
        HttpClient Client { get; }

        public SonarrDownloadClient(HttpClient client)
        {
            this.Client = client;
        }

        public SonarrResponse<string> DownloadToPath(string url, string path, CancellationToken token = default)
        {
            using HttpRequestMessage msg = new(HttpMethod.Get, url);
            msg.Options.Set(CookieHandler.NoCookie, true);

            HttpResponseMessage response = null!;
            try
            {
                response = this.Client.Send(msg, token);
            }
            catch (SonarrHttpException ex)
            {
                var result = SonarrResponse.FromException<string>(
                    url, ex, ErrorCategory.AuthenticationError, HttpStatusCode.Unauthorized, response);

                return result;
            }

            this.WriteFileAsync(response, path, token).GetAwaiter().GetResult();

            return new SonarrResponse<string>(url, path, null, HttpStatusCode.OK);
        }
        public SonarrResponse<string> DownloadToPath(string url, string path, NetworkCredential? credential, CancellationToken token = default)
        {
            using HttpRequestMessage msg = new(HttpMethod.Get, url);
            msg.Options.Set(CookieHandler.CredentialKey, credential);
            msg.Options.Set(CookieHandler.NoCookie, false);

            HttpResponseMessage response = null!;
            try
            {
                response = this.Client.Send(msg, token);
            }
            catch (SonarrHttpException ex)
            {
                var result = SonarrResponse.FromException<string>(
                    url, ex, ErrorCategory.AuthenticationError, HttpStatusCode.Unauthorized, response);

                return result;
            }

            this.WriteFileAsync(response, path, token).GetAwaiter().GetResult();

            return new SonarrResponse<string>(url, path, null, HttpStatusCode.OK);
        }

        private async Task WriteFileAsync(HttpResponseMessage response, string path, CancellationToken token)
        {
            await using Stream stream = response.Content.ReadAsStream(token);

            await using FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            await stream.CopyToAsync(fs, token);
        }
    }

    internal static class SonarrDownloadClientDependencyInjection
    {
        internal static IServiceCollection AddSonarrDownloadClient(this IServiceCollection services)
        {
            services
                .AddTransient<CookieHandler>()
                .AddHttpClient<ISonarrDownloadClient, SonarrDownloadClient>((provider, client) =>
                {
                    var settings = provider.GetRequiredService<IConnectionSettings>();
                    client.BaseAddress = settings.ServiceUri;
                    client.Timeout = settings.Timeout;
                    client.DefaultRequestHeaders
                        .Add(SonarrClientDependencyInjection.API_HEADER_KEY, settings.ApiKey.GetValue());

                    client.DefaultRequestHeaders.UserAgent
                        .Add(SonarrClientDependencyInjection.UserAgent);
                })
                .ConfigurePrimaryHttpMessageHandler<SonarrClientHandler>()
                .AddHttpMessageHandler<VerboseHandler>()
                .AddHttpMessageHandler<PathHandler>()
                .AddHttpMessageHandler<CookieHandler>();

            return services;
        }
    }
}