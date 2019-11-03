using MG.Sonarr.Functionality;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace MG.Sonarr
{
    /// <summary>
    /// A Sonarr-specific implementation of the <see cref="HttpClient"/> class used to issue RESTful API requests.
    /// </summary>
    public class SonarrRestClient : HttpClient, ISonarrClient
    {
        #region CONSTRUCTORS
        /// <summary>
        /// Initializes a new instance of the <see cref="SonarrRestClient"/> class.
        /// </summary>
        public SonarrRestClient() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SonarrRestClient"/> class with the specified url-string for its base.
        /// </summary>
        /// <param name="baseUrl">The url as a string to use as the <see cref="HttpClient.BaseAddress"/>.</param>
        public SonarrRestClient(string baseUrl) : base()
        {
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri useThis))
            {
                base.BaseAddress = useThis;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SonarrRestClient"/> class with the specified <see cref="Uri"/> for its base address.
        /// </summary>
        /// <param name="baseUri">The <see cref="Uri"/> to use as the <see cref="HttpClient.BaseAddress"/>.</param>
        public SonarrRestClient(Uri baseUri) : base() => base.BaseAddress = baseUri;

        /// <summary>
        /// Initializes a new instance of the <see cref="SonarrRestClient"/> class that sets the following <see cref="IApiKey"/> 
        /// to be used for authentication.
        /// </summary>
        /// <param name="apiKey">The <see cref="IApiKey"/> implementing class to added in the <see cref="WebHeaderCollection"/>.</param>
        public SonarrRestClient(IApiKey apiKey) : base() => this.AddApiKey(apiKey);

        /// <summary>
        /// Initializes a new instance of the <see cref="SonarrRestClient"/> class while adding the specified 
        /// url-string for its base address and <see cref="IApiKey"/> to the <see cref="WebHeaderCollection"/>
        /// for authentication.
        /// </summary>
        /// <param name="baseUrl">The url as a string to use as the <see cref="HttpClient.BaseAddress"/>.</param>
        /// <param name="apiKey">The <see cref="IApiKey"/> implementing class to added in the <see cref="WebHeaderCollection"/>.</param>
        public SonarrRestClient(string baseUrl, IApiKey apiKey) : this(baseUrl) => this.AddApiKey(apiKey);

        /// <summary>
        /// Initializes a new instance of the <see cref="SonarrRestClient"/> class while adding the specified 
        /// <see cref="Uri"/> for its base address and <see cref="IApiKey"/> to the <see cref="WebHeaderCollection"/>
        /// for authentication.        
        /// </summary>
        /// <param name="baseUri">The <see cref="Uri"/> to use as the <see cref="HttpClient.BaseAddress"/>.</param>
        /// <param name="apiKey">The <see cref="IApiKey"/> implementing class to added in the <see cref="WebHeaderCollection"/>.</param>
        public SonarrRestClient(Uri baseUri, IApiKey apiKey) : this(baseUri) => this.AddApiKey(apiKey);

        /// <summary>
        /// Initializes a new instance of the <see cref="SonarrRestClient"/> class with a specified handler.
        /// </summary>
        /// <param name="handler">The handler added to the base <see cref="HttpClient"/>.</param>
        public SonarrRestClient(HttpMessageHandler handler) : base(handler) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SonarrRestClient"/> class with a specified handler while
        /// adding a <see cref="IApiKey"/> to the <see cref="HttpClient.DefaultRequestHeaders"/> collection.
        /// </summary>
        /// <param name="handler">The handler added to the base <see cref="HttpClient"/>.</param>
        /// <param name="apiKey">The <see cref="IApiKey"/> implementing class to added in the <see cref="WebHeaderCollection"/>.</param>
        public SonarrRestClient(HttpMessageHandler handler, IApiKey apiKey) : base(handler) => this.AddApiKey(apiKey);

        /// <summary>
        /// Initializes a new instance of the <see cref="SonarrRestClient"/> class with a specified handler.
        /// </summary>
        /// <param name="handler">The handler added to the base <see cref="HttpClient"/>.</param>
        /// <param name="disposeHandler">Specifies to dispose the handler after its applied.</param>
        public SonarrRestClient(HttpMessageHandler handler, bool disposeHandler)
            : base(handler, disposeHandler) { }

        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Adds the specified <see cref="IApiKey"/> to the <see cref="HttpClient.DefaultRequestHeaders"/> collection.
        /// </summary>
        /// <param name="apiKey">The api key to add to the <see cref="HttpClient.DefaultRequestHeaders"/> collection.</param>
        public void AddApiKey(IApiKey apiKey)
        {
            ValueTuple<string, string> kvp = apiKey.ToTuple();
            base.DefaultRequestHeaders.Add(kvp.Item1, kvp.Item2);
        }

        bool ISonarrClient.IsJsonArray(string jsonString)
        {
            var jtok = JToken.Parse(jsonString);
            return jtok is JArray
                ? true
                : false;
        }

        #endregion
    }
}