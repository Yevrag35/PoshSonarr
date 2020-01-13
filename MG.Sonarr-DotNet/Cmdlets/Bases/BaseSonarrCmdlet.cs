using MG.Api.Json;
using MG.Api.Json.Extensions;
using MG.Api.Rest;
using MG.Api.Rest.Generic;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Cmdlets
{
    /// <summary>
    /// The main base <see cref="PSCmdlet"/> class for all PoshSonarr cmdlets.  Includes custom API methods along with advanced error-handling.
    /// </summary>
    public abstract partial class BaseSonarrCmdlet : PSCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string CONNECT_EP = "/system/status";
        [Obsolete]
        private const string CONTENT_TYPE = "application/json";
        private const string DEBUG_API_MSG = "Sending {0} request to: {1}{2}";
        private const string DEBUG_API_AND_BODY_MSG = DEBUG_API_MSG + "{3}{3}REQUEST BODY:{4}";
        private const string DEBUG_API_RESPONSE_MSG = "RESPONSE ({0} {1}): {2}{2}{3}";

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            if (!Context.IsConnected)
                throw new SonarrContextNotSetException();
        }

        #endregion

        #region PIPELINE METHOD
        /// <summary>
        /// Sends an <see cref="object"/> to the PowerShell console and optionally specifies whether or not to enumerate it
        /// if it's a collection.
        /// </summary>
        /// <param name="obj">The object to send to the pipeline</param>
        /// <param name="enumerateCollection">Indicates whether the object will be enumerated as its sent.</param>
        protected void SendToPipeline(object obj, bool enumerateCollection = true)
        {
            if (obj != null)
            {
                base.WriteObject(obj, enumerateCollection);
            }
        }

        #endregion

        #region SHOULD PROCESS METHODS
        protected bool FormatShouldProcess(string action, string stringFormat, params object[] arguments)
        {
            string msg = string.Format(stringFormat, arguments);
            return base.ShouldProcess(msg, action);
        }

        #endregion

        #region OLD API METHODS

        /// <summary>
        /// Sends a GET request to the specified Sonarr endpoint and returning a JSON-formatted string in response.  This is for exclusive use with <see cref="ConnectInstance"/>.  
        /// Errors are handled by <see cref="PSCmdlet"/>.WriteError.
        /// </summary>
        /// <exception cref="Exception"/>
        [Obsolete]
        protected string TrySonarrConnect()
        {
            this.WriteApiDebug(CONNECT_EP, HttpMethod.Get, out string apiPath);

            HttpResponseMessage task = Context.ApiCaller.GetAsync(apiPath, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false).GetAwaiter().GetResult();
            string res = null;
            try
            {
                using (HttpResponseMessage resp = task.EnsureSuccessStatusCode())
                {
                    using (HttpContent content = resp.Content)
                    {
                        Task<string> strTask = content.ReadAsStringAsync();
                        strTask.Wait();
                        res = strTask.Result;
                    }
                }
            }
            catch (HttpRequestException hre)
            {
                this.WriteError(new SonarrConnectException(apiPath, hre), ErrorCategory.ParserError);
                return null;
            }
            catch (Exception e)
            {
                throw this.GetAbsoluteException(e);
            }

            return res;
        }

        /// <summary>
        /// Sends a DELETE request to the specified Sonarr endpoint.  Errors are handled by <see cref="PSCmdlet"/>.WriteError.
        /// </summary>
        /// <param name="endpoint">The Sonarr API endpoint to send the DELETE request to.</param>
        [Obsolete]
        protected void TryDeleteSonarrResult(string endpoint)
        {
            this.WriteApiDebug(endpoint, HttpMethod.Delete, out string apiPath);

            try
            {
                Task<HttpResponseMessage> task = Context.ApiCaller.DeleteAsync(apiPath);
                task.Wait();
                task.Result.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException hre)
            {
                this.WriteError(new SonarrDeleteRequestException(apiPath, hre), ErrorCategory.MetadataError, endpoint);
            }
        }

        /// <summary>
        /// Sends a GET request to the specified Sonarr endpoint returning a JSON-formatted string in response.  
        /// Errors are handled by <see cref="PSCmdlet"/>.WriteError.
        /// </summary>
        /// <param name="endpoint">The Sonarr API endpoint to send the GET request to.</param>
        [Obsolete]
        protected string TryGetSonarrResult(string endpoint, bool showAllDebug = true)
        {
            this.WriteApiDebug(endpoint, HttpMethod.Get, out string apiPath);

            try
            {
                HttpResponseMessage task = Context.ApiCaller
                    .GetAsync(apiPath, HttpCompletionOption.ResponseContentRead)
                        .ConfigureAwait(false)
                            .GetAwaiter()
                                .GetResult();
                string res = null;
                using (HttpResponseMessage resp = task.EnsureSuccessStatusCode())
                {
                    using (HttpContent content = resp.Content)
                    {
                        res = content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                        this.WriteApiDebug(res, resp.StatusCode, showAllDebug);
                    }
                }
                return res;
            }
            catch (HttpRequestException hre)
            {
                this.WriteError(new SonarrGetRequestException(apiPath, hre), ErrorCategory.ParserError);
                return null;
            }
        }

        /// <summary>
        /// Sends a POST request to the specified Sonarr endpoint with the specified string payload formatted in JSON 
        /// returning a JSON-formatted string in response.  Errors are handled by <see cref="PSCmdlet"/>.WriteError.
        /// </summary>
        /// <param name="endpoint">The Sonarr API endpoint to send the POST request to.</param>
        /// <param name="jsonBody">The JSON payload content to be sent with the POST request.</param>
        [Obsolete]
        protected string TryPostSonarrResult(string endpoint, IJsonResult jsonBody)
        {
            return this.TryPostSonarrResult(endpoint, jsonBody.ToJson());
        }

        /// <summary>
        /// Sends a POST request to the specified Sonarr endpoint with the specified string payload formatted in JSON 
        /// returning a JSON-formatted string in response.  Errors are handled by <see cref="PSCmdlet"/>.WriteError.
        /// </summary>
        /// <param name="endpoint">The Sonarr API endpoint to send the POST request to.</param>
        /// <param name="jsonBody">The JSON payload content to be sent with the POST request.</param>
        [Obsolete]
        protected string TryPostSonarrResult(string endpoint, string jsonBody)
        {
            this.WriteApiDebug(endpoint, HttpMethod.Post, jsonBody, out string apiPath);

            StringContent sc = null;
            if (!string.IsNullOrEmpty(jsonBody))
                sc = new StringContent(jsonBody, Encoding.UTF8, CONTENT_TYPE);

            try
            {
                Task<HttpResponseMessage> task = Context.ApiCaller.PostAsync(apiPath, sc);
                task.Wait();

                string res = null;
                using (HttpResponseMessage resp = task.Result.EnsureSuccessStatusCode())
                {
                    using (HttpContent content = resp.Content)
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
                this.WriteError(new SonarrPostRequestException(apiPath, hre), ErrorCategory.InvalidArgument, jsonBody);
                return null;
            }
        }

        /// <summary>
        /// Sends a PUT request to the specified Sonarr endpoint with the specified string payload formatted in JSON 
        /// returning a JSON-formatted string in response.  Errors are handled by <see cref="PSCmdlet"/>.WriteError.
        /// </summary>
        /// <param name="endpoint">The Sonarr API endpoint to send the PUT request to.</param>
        /// <param name="jsonBody">The JSON payload content to be sent with the PUT request.</param>
        [Obsolete]
        protected string TryPutSonarrResult(string endpoint, IJsonResult jsonBody)
        {
            return this.TryPutSonarrResult(endpoint, jsonBody.ToJson());
        }

        /// <summary>
        /// Sends a PUT request to the specified Sonarr endpoint with the specified string payload formatted in JSON 
        /// returning a JSON-formatted string in response.  Errors are handled by <see cref="PSCmdlet"/>.WriteError.
        /// </summary>
        /// <param name="endpoint">The Sonarr API endpoint to send the PUT request to.</param>
        /// <param name="jsonBody">The JSON payload content to be sent with the PUT request.</param>
        [Obsolete]
        protected string TryPutSonarrResult(string endpoint, string jsonBody)
        {
            this.WriteApiDebug(endpoint, HttpMethod.Put, jsonBody, out string apiPath);

            StringContent sc = null;
            if (!string.IsNullOrEmpty(jsonBody))
                sc = new StringContent(jsonBody, Encoding.UTF8, CONTENT_TYPE);

            try
            {
                Task<HttpResponseMessage> task = Context.ApiCaller.PutAsync(apiPath, sc);
                task.Wait();

                string res = null;
                using (HttpResponseMessage resp = task.Result.EnsureSuccessStatusCode())
                {
                    using (HttpContent content = resp.Content)
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
                this.WriteError(new SonarrPutRequestException(apiPath, hre), ErrorCategory.InvalidArgument, jsonBody);
                return null;
            }
        }

        #endregion
    }
}