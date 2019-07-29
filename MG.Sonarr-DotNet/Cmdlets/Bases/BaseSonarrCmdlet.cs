using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Cmdlets
{
    public abstract class BaseSonarrCmdlet : PSCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string API_PREFIX = "/api";
        private const string CONTENT_TYPE = "application/json";

        protected private ApiCaller _api;
        protected private bool _noPre;

        protected private static readonly JsonSerializerSettings Serializer = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.DateTime,
            DateTimeZoneHandling = DateTimeZoneHandling.Local,
            DefaultValueHandling = DefaultValueHandling.Populate,
            FloatParseHandling = FloatParseHandling.Decimal,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize
        };

        #endregion

        #region PARAMETERS


        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            if (!Context.IsConnected)
                throw new SonarrContextNotSetException();

            else
            {
                _api = Context.ApiCaller;
                _noPre = Context.NoApiPrefix;
            }
        }

        #endregion

        #region API METHODS
        protected string SonarrGet(string sonarrEndpoint)
        {
            if (!Context.NoApiPrefix)
                sonarrEndpoint = API_PREFIX + sonarrEndpoint;

            Task<HttpResponseMessage> task = _api.GetAsync(sonarrEndpoint, HttpCompletionOption.ResponseContentRead);
            task.Wait();
            string res = null;
            if (!task.IsFaulted && !task.IsCanceled)
            {
                using (HttpResponseMessage resp = task.Result.EnsureSuccessStatusCode())
                {
                    using (var content = resp.Content)
                    {
                        Task<string> strTask = content.ReadAsStringAsync();
                        strTask.Wait();
                        res = strTask.Result;
                    }
                }
            }

            return res;
        }

        #endregion

        #region BACKEND METHODS
        protected private void TryDeleteSonarrResult(string endpoint)
        {
            try
            {
                base.WriteVerbose(string.Format("DELETE REQUEST URL: {0}", endpoint));
                _api.SonarrDelete(endpoint);
            }
            catch (Exception e)
            {
                base.WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.InvalidArgument, endpoint));
            }
        }

        protected private string TryGetSonarrResult(string endpoint)
        {
            try
            {
                base.WriteVerbose(string.Format("GET REQUEST URL: {0}", endpoint));
                string strRes = _api.SonarrGet(endpoint);
                return strRes;
            }
            catch (Exception e)
            {
                base.WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.ReadError, endpoint));
                return null;
            }
        }

        protected private string TryPostSonarrResult(string endpoint, string jsonBody)
        {
            try
            {
                base.WriteVerbose(string.Format("POST REQUEST URL: {0}", endpoint));
                base.WriteDebug("POST BODY:" + Environment.NewLine + jsonBody);
                return _api.SonarrPost(endpoint, jsonBody);
            }
            catch (Exception e)
            {
                base.WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.InvalidArgument, endpoint));
                return null;
            }
        }

        protected private string TryPutSonarrResult(string endpoint, string jsonBody)
        {
            try
            {
                base.WriteVerbose(string.Format("PUT REQUEST URL: {0}", endpoint));
                base.WriteDebug("PUT BODY:" + Environment.NewLine + jsonBody);
                return _api.SonarrPut(endpoint, jsonBody);
            }
            catch (Exception e)
            {
                base.WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.InvalidArgument, endpoint));
                return null;
            }
        }

        protected private void WriteError(string msg, ErrorCategory cat) =>
            this.WriteError(new ArgumentException(msg), cat, null);

        protected private void WriteError(string msg, ErrorCategory cat, object obj) =>
            this.WriteError(new ArgumentException(msg), cat, obj);

        protected private void WriteError(string msg, Exception exception, ErrorCategory cat, object obj)
        {
            var errRec = new ErrorRecord(new InvalidOperationException(msg, exception), exception.GetType().FullName, cat, obj);
            base.WriteError(errRec);
        }

        protected private void WriteError(Exception baseEx, ErrorCategory cat) => this.WriteError(baseEx, cat, null);
        protected private void WriteError(Exception baseEx, ErrorCategory cat, object obj)
        {
            var errRec = new ErrorRecord(baseEx, baseEx.GetType().FullName, cat, obj);
            base.WriteError(errRec);
        }

        #endregion
    }
}