//using MG.Sonarr.Functionality.Extensions;
using MG.Api.Json;
using MG.Api.Rest.Generic;
using MG.Sonarr.Functionality.Internal;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace MG.Sonarr.Functionality
{
    public static class ClassFactory
    {
        internal static ISonarrClient GenerateClient(HttpClientHandler handler, ISonarrUrl url, IApiKey apiKey, bool allowRedirects, WebProxy proxy = null)
        {
            if (proxy != null)
                handler.Proxy = proxy;

            handler.AllowAutoRedirect = allowRedirects;
            var client = new SonarrRestClient(handler)
            {
                BaseAddress = url.Url
            };
            client.AddApiKey(apiKey);
            return client;
        }
        public static ISonarrClient GenerateClient(HttpClientHandler handler, ISonarrUrl url, IApiKey apiKey, bool allowRedirects,
            string proxyUrl, ICredentials proxyCredentials, bool bypassOnLocal)
        {
            WebProxy proxy = null;
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                proxy = new WebProxy(proxyUrl, bypassOnLocal);
                if (proxyCredentials != null)
                    proxy.Credentials = proxyCredentials;

                else
                    proxy.UseDefaultCredentials = true;
            }
            return GenerateClient(handler, url, apiKey, allowRedirects, proxy);
        }
        public static ISonarrUrl GenerateSonarrUrl(Uri url, bool includeApiPrefix) => new SonarrUrl(url, includeApiPrefix);
        public static ISonarrUrl GenerateSonarrUrl(string hostName, int portNumber, bool useSsl, string reverseProxyBase, bool includeApiPrefix)
        {
            return new SonarrUrl(hostName, portNumber, useSsl, reverseProxyBase, includeApiPrefix);
        }
        public static Task<ITagManager> GenerateTagManagerAsync(ISonarrClient client, bool addApiToPath) => TagManager.GenerateAsync(client, addApiToPath);
        public static IEqualityComparer<string> NewIgnoreCase() => new IgnoreCase();
        public static IJobHistory NewJobHistory() => new JobHistoryRepository();
    }
}
