using MG.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Sonarr.Api
{
    public class ApiCaller : GenericApi
    {
        #region Properties/Fields/Constants
        private protected const string _ct = "application/json";
        private readonly string _base;
        public string BaseUrl => _base;

        #endregion

        #region Constructors
        public ApiCaller(string baseUrl) => _base = baseUrl;

        #endregion

        #region Methods
        public ApiResult Send(ISonarrEndpoint endpoint, ApiKey key, 
            IDictionary reqProps = null, SonarrMethod method = SonarrMethod.GET, RequestParameters reqParams = null)
        {
            if (!endpoint.MethodsAllowed.Contains(method))
            {
                throw new ArgumentException(method.ToString() + " is not a valid request method for this endpoint!");
            }
            string jsonBody = null;
            if (reqParams != null)
            {
                jsonBody = reqParams.JsonContent;
            }
            if (reqProps == null)
            {
                reqProps = new Dictionary<string, object>();
            }
            reqProps.Add("Method", method.ToString());
            reqProps.Add("Headers", key.AsSonarrHeader());
            reqProps.Add("ContentType", _ct);
            var full = _base + endpoint.Value;
            return ReturnWebRequest(full, reqProps, jsonBody);
        }


        public override void SaveData(string valueName, object valueData) => throw new NotImplementedException();

        #endregion
    }
}
