using MG.Sonarr.Functionality.Client;
using MG.Sonarr.Functionality.Jobs;
using MG.Sonarr.Functionality.Strings;
using MG.Sonarr.Functionality.Tags;
using MG.Sonarr.Functionality.Url;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace MG.Sonarr.Functionality
{
    /// <summary>
    /// A static factory class for generating new instances of internal functional classes.
    /// </summary>
    public static class SonarrFactory
    {
        #region SONARR CLIENT
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
        /// <summary>
        /// Creates a new <see cref="ISonarrClient"/> instance with the provided handler and details.
        /// </summary>
        /// <param name="handler">The <see cref="HttpClientHandler"/> used when constructing the internal HTTP client.</param>
        /// <param name="url">The URL of the Sonarr instance the <see cref="ISonarrClient"/> will target.</param>
        /// <param name="apiKey">The api key used in all RESTful requests for authentication.</param>
        /// <param name="allowRedirects">Instructs the <see cref="ISonarrClient"/> whether or not it should follow redirects.</param>
        /// <param name="proxyUrl">A proxy URL used by the <see cref="ISonarrClient"/> when sending requests.</param>
        /// <param name="proxyCredentials">A set of credentials to authenticate to the proxy.</param>
        /// <param name="bypassOnLocal">Indicates to bypass the proxy when a destination is determined to be local to the client.</param>
        /// <returns>An <see cref="ISonarrClient"/> instance with the specified settings.</returns>
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

        #endregion

        #region SONARR URL
        /// <summary>
        /// Constructs a new <see cref="ISonarrUrl"/> instance from the given <see cref="Uri"/> and prefix preference.
        /// </summary>
        /// <param name="url">The <see cref="Uri"/> that's used for connecting to a Sonarr instance.</param>
        /// <param name="includeApiPrefix">Indicates whether or not all RESTful requests should append "/api".</param>
        /// <returns>An <see cref="ISonarrUrl"/> to the specified Sonarr instance.</returns>
        public static ISonarrUrl GenerateSonarrUrl(Uri url, bool includeApiPrefix) => new SonarrUrl(url, includeApiPrefix);
        /// <summary>
        /// Constructs a new <see cref="ISonarrUrl"/> instance from the given parameters, reverse proxy base URL, 
        /// and prefix perference.  A new <see cref="Uri"/> is created
        /// from the specified hostname, port, and SSL usage.  
        /// </summary>
        /// <param name="hostName">The hostname where the Sonarr instance is located.</param>
        /// <param name="portNumber">The port number used in the connection.</param>
        /// <param name="useSsl">Indicates whether HTTPS will be used.</param>
        /// <param name="reverseProxyBase">A base URL when connecting to Sonarr with a reverse proxy.</param>
        /// <param name="includeApiPrefix">Indicates whether or not all RESTful requests should append "/api".</param>
        /// <returns>An <see cref="ISonarrUrl"/> to the specified Sonarr instance.</returns>
        public static ISonarrUrl GenerateSonarrUrl(string hostName, int portNumber, bool useSsl, string reverseProxyBase, bool includeApiPrefix)
        {
            return new SonarrUrl(hostName, portNumber, useSsl, reverseProxyBase, includeApiPrefix);
        }

        #endregion

        #region TAG MANAGER
        /// <summary>
        /// Initializes a new instance of <see cref="ITagManager"/> to manage <see cref="Tag"/> objects within Sonarr.
        /// </summary>
        /// <param name="client">The HTTP client used in all tag API operations.</param>
        /// <param name="addApiToPath">Indicates whether or not all RESTful requests should append "/api".</param>
        /// <returns>An <see cref="ITagManager"/> instance.</returns>
        public static Task<ITagManager> GenerateTagManagerAsync(ISonarrClient client, bool addApiToPath) => TagManager.GenerateAsync(client, addApiToPath);

        #endregion

        #region MISCELLANEOUS
        /// <summary>
        /// Initializes a new <see cref="IEqualityComparer{T}"/> of <see cref="string"/> objects that does not
        /// look at case for equality.
        /// </summary>
        /// <returns>An <see cref="IEqualityComparer{T}"/> of <see cref="string"/>s that ignores case when determining equality.</returns>
        public static IEqualityComparer<string> NewIgnoreCase() => new IgnoreCase();
        /// <summary>
        /// Initializes a new <see cref="IJobHistory"/> class that manages command results sent to Sonarr.
        /// </summary>
        public static IJobHistory NewJobHistory() => new JobHistoryRepository();

        #endregion
    }
}
