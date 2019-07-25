using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;

namespace MG.Sonarr
{
    public class ApiCaller : HttpClient
    {
        #region FIELDS/CONSTANTS


        #endregion

        #region PROPERTIES


        #endregion

        #region CONSTRUCTORS
        public ApiCaller()
            : base() { }
        public ApiCaller(string baseUrl)
            : base()
        {
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri useThis))
            {
                base.BaseAddress = useThis;
            }
        }
        public ApiCaller(Uri baseUri)
            : base() => base.BaseAddress = baseUri;
        public ApiCaller(ApiKey apiKey)
            : base() => this.AddApiKey(apiKey);
        public ApiCaller(string baseUrl, ApiKey apiKey)
            : this(baseUrl) => this.AddApiKey(apiKey);
        public ApiCaller(Uri baseUri, ApiKey apiKey)
            : this(baseUri) => this.AddApiKey(apiKey);

        public ApiCaller(HttpMessageHandler handler) 
            : base(handler) { }
        public ApiCaller(HttpMessageHandler handler, ApiKey apiKey)
            : base(handler) => this.AddApiKey(apiKey);

        public ApiCaller(HttpMessageHandler handler, bool disposeHandler)
            : base(handler, disposeHandler) { }

        #endregion

        #region PUBLIC METHODS
        public void AddApiKey(ApiKey apiKey)
        {
            KeyValuePair<string, string> kvp = apiKey.AsKeyValuePair();
            base.DefaultRequestHeaders.Add(kvp.Key, kvp.Value);
        }

        #endregion

        #region BACKEND/PRIVATE METHODS


        #endregion
    }
}