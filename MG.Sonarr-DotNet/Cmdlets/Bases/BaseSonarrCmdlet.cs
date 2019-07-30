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
        protected string TrySonarrConnect(string sonarrEndpoint)
        {
            sonarrEndpoint = Context.UriBase + sonarrEndpoint;

            Task<HttpResponseMessage> task = Context.ApiCaller.GetAsync(sonarrEndpoint, HttpCompletionOption.ResponseContentRead);
            task.Wait();
            string res = null;
            if (!task.IsFaulted && !task.IsCanceled)
            {
                try
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
                catch (HttpRequestException hre)
                {
                    this.WriteError(new SonarrConnectException(sonarrEndpoint, hre), ErrorCategory.ParserError);
                    return null;
                }
                catch (Exception e)
                {
                    throw this.GetAbsoluteException(e);
                }
            }

            return res;
        }

        protected void TryDeleteSonarrResult(string endpoint)
        {
            endpoint = Context.UriBase + endpoint;
            base.WriteVerbose(string.Format("DELETE REQUEST URL: {0}", endpoint));
            try
            {
                Task<HttpResponseMessage> task = Context.ApiCaller.DeleteAsync(endpoint);
                task.Wait();
                task.Result.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException hre)
            {
                this.WriteError(new SonarrDeleteRequestException(endpoint, hre), ErrorCategory.MetadataError, endpoint);
            }
        }

        protected string TryGetSonarrResult(string endpoint)
        {
            endpoint = Context.UriBase + endpoint;
            base.WriteVerbose(string.Format("GET REQUEST URL: {0}", endpoint));
            try
            {
                Task<HttpResponseMessage> task = Context.ApiCaller.GetAsync(endpoint, HttpCompletionOption.ResponseContentRead);
                task.Wait();
                string res = null;
                using (var resp = task.Result.EnsureSuccessStatusCode())
                {
                    using (var content = resp.Content)
                    {
                        Task<string> strTask = content.ReadAsStringAsync();
                        strTask.Wait();
                        res = strTask.Result;
                    }
                }
                return res;
            }
            catch (HttpRequestException hre)
            {
                this.WriteError(new SonarrGetRequestException(endpoint, hre), ErrorCategory.ParserError);
                return null;
            }
        }

        protected string TryPostSonarrResult(string endpoint, string jsonBody)
        {
            endpoint = Context.UriBase + endpoint;
            base.WriteVerbose(string.Format("POST REQUEST URL: {0}", endpoint));
            base.WriteDebug("POST BODY:" + Environment.NewLine + jsonBody);

            StringContent sc = null;
            if (!string.IsNullOrEmpty(jsonBody))
                sc = new StringContent(jsonBody, Encoding.UTF8, CONTENT_TYPE);

            try
            {
                Task<HttpResponseMessage> task = Context.ApiCaller.PostAsync(endpoint, sc);
                task.Wait();

                string res = null;
                using (HttpResponseMessage resp = task.Result.EnsureSuccessStatusCode())
                {
                    using (var content = resp.Content)
                    {
                        Task<string> strTask = content.ReadAsStringAsync();
                        strTask.Wait();
                        res = strTask.Result;
                    }
                }
                return res;
            }
            catch (HttpRequestException hre)
            {
                this.WriteError(new SonarrPostRequestException(endpoint, hre), ErrorCategory.InvalidArgument, jsonBody);
                return null;
            }
        }

        protected string TryPutSonarrResult(string endpoint, string jsonBody)
        {
            endpoint = Context.UriBase + endpoint;
            base.WriteVerbose(string.Format("PUT REQUEST URL: {0}", endpoint));
            base.WriteDebug("PUT BODY:" + Environment.NewLine + jsonBody);

            StringContent sc = null;
            if (!string.IsNullOrEmpty(jsonBody))
                sc = new StringContent(jsonBody, Encoding.UTF8, CONTENT_TYPE);

            try
            {
                Task<HttpResponseMessage> task = Context.ApiCaller.PutAsync(endpoint, sc);
                task.Wait();

                string res = null;
                using (HttpResponseMessage resp = task.Result.EnsureSuccessStatusCode())
                {
                    using (var content = resp.Content)
                    {
                        Task<string> strTask = content.ReadAsStringAsync();
                        strTask.Wait();
                        res = strTask.Result;
                    }
                }
                return res;
            }
            catch (HttpRequestException hre)
            {
                this.WriteError(new SonarrPutRequestException(endpoint, hre), ErrorCategory.InvalidArgument, jsonBody);
                return null;
            }
        }

        #endregion

        #region EXCEPTION METHODS
        protected virtual Exception GetAbsoluteException(Exception e)
        {
            while (e.InnerException != null)
            {
                e = e.InnerException;
            }
            return e;
        }

        protected void WriteError(string msg, ErrorCategory cat) =>
            this.WriteError(new ArgumentException(msg), cat, null);

        protected void WriteError(string msg, ErrorCategory cat, object obj) =>
            this.WriteError(new ArgumentException(msg), cat, obj);

        protected void WriteError(string msg, Exception exception, ErrorCategory cat, object obj)
        {
            exception = this.GetAbsoluteException(exception);

            var errRec = new ErrorRecord(new InvalidOperationException(msg, exception), exception.GetType().FullName, cat, obj);
            base.WriteError(errRec);
        }

        protected void WriteError(Exception baseEx, ErrorCategory cat) => this.WriteError(baseEx, cat, null);
        protected void WriteError(Exception baseEx, ErrorCategory cat, object obj)
        {
            //baseEx = this.GetAbsoluteException(baseEx);
            var errRec = new ErrorRecord(baseEx, baseEx.GetType().FullName, cat, obj);
            base.WriteError(errRec);
        }

        #endregion
    }
}