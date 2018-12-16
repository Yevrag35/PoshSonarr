using MG.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace Sonarr.Api
{
    public class ApiCaller : Caller
    {
        #region Properties/Fields/Constants
        private const string CONTENT_TYPE = "application/json";
        private static readonly Encoding ENCODING = Encoding.UTF8;
        private const string HEADER_KEY = "X-Api-Key";
        private readonly string _base;
        public string BaseUrl => _base;
        public bool NoApiPrefix => SonarrServiceContext.NoApiPrefix;

        #endregion

        #region Constructors
        public ApiCaller(string baseUrl)
        {
            this.Headers.Add(HEADER_KEY, SonarrServiceContext.ApiKey.Value);

            if (baseUrl.EndsWith("/"))
                baseUrl = baseUrl.Substring(0, baseUrl.LastIndexOf("/"));

            this.hpc.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            SonarrServiceContext.BaseUrl = baseUrl;
        }

        #endregion

        #region Methods
        private ApiUrl GetUrl(ISonarrEndpoint ep)
        {
            var full = SonarrServiceContext.BaseUrl + ep.Value;
            if (SonarrServiceContext.NoApiPrefix)
                full = full.Replace("/api", string.Empty);

            return full;
        }

        public IEnumerable<T> SonarrGetAs<T>(ISonarrEndpoint endpoint) where T : SonarrResult
        {
            var useUrl = GetUrl(endpoint);
            var thisObj = Activator.CreateInstance<T>();
            return base.GetAs<T>(useUrl, thisObj.SkipThese);
        }

        public void SonarrPost(ISonarrPostable endpoint)
        {
            var useUrl = GetUrl(endpoint);
            var contentBody = endpoint.GetPostBody();
            base.NoResponsePost(useUrl, CONTENT_TYPE, ENCODING, contentBody);
        }

        public IEnumerable<T> SonarrPostAs<T>(ISonarrPostable endpoint) where T : SonarrResult
        {
            var useUrl = GetUrl(endpoint);
            var contentBody = endpoint.GetPostBody();
            var thisObj = Activator.CreateInstance<T>();

            var result = base.PostAs<T>(useUrl, CONTENT_TYPE, ENCODING, contentBody, thisObj.SkipThese);
            return result;
        }

        public void SonarrDelete(ISonarrPostable endpoint)
        {
            if (endpoint.UsingMethod != SonarrMethod.DELETE)
                throw new InvalidOperationException("The endpoint specified does not have its 'method' explicitly set to DELETE!");

            var useUrl = GetUrl(endpoint);
            this.Delete(useUrl);
        }

        //public ApiResult Send(ISonarrEndpoint endpoint, 
        //    IDictionary reqProps = null, SonarrMethod method = SonarrMethod.GET, RequestParameters reqParams = null)
        //{
        //    var key = SonarrServiceContext.ApiKey;
        //    if (!endpoint.MethodsAllowed.Contains(method))
        //    {
        //        throw new ArgumentException(method.ToString() + " is not a valid request method for this endpoint!");
        //    }
        //    string jsonBody = null;
        //    if (reqParams != null)
        //    {
        //        jsonBody = reqParams.JsonContent;
        //    }
        //    if (reqProps == null)
        //    {
        //        reqProps = new Dictionary<string, object>();
        //    }
        //    reqProps.Add("Method", method.ToString());
        //    reqProps.Add("Headers", key.AsSonarrHeader());
        //    reqProps.Add("ContentType", _ct);
        //    var full = _base + endpoint.Value;
        //    if (NoApiPrefix)
        //        full = full.Replace("/api", string.Empty);

        //    return ReturnWebRequest(full, reqProps, jsonBody);
        //}

        //public ApiResult Send(ISonarrEndpoint endpoint, string jsonBody, SonarrMethod method, IDictionary reqProps = null)
        //{
        //    if (method == SonarrMethod.GET)
        //        throw new Exception("Cannot use this overload with the \"GET\" method.");

        //    if (!endpoint.MethodsAllowed.Contains(method))
        //        throw new ArgumentException(method.ToString() + " is not a valid request method for this endpoint!");

        //    if (reqProps == null)
        //        reqProps = new Dictionary<string, object>();

        //    var key = SonarrServiceContext.ApiKey;
        //    reqProps.Add("Method", method.ToString());
        //    reqProps.Add("Headers", key.AsSonarrHeader());
        //    reqProps.Add("ContentType", _ct);
        //    var full = _base + endpoint.Value;
        //    if (NoApiPrefix)
        //        full = full.Replace("/api", string.Empty);

        //    return ReturnWebRequest(full, reqProps, jsonBody);
        //}

        //public override void SaveData(string valueName, object valueData) => throw new NotImplementedException();

        #endregion
    }
}
