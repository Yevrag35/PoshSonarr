using MG.Api.Json;
using MG.Api.Json.Extensions;
using MG.Api.Rest;
using MG.Api.Rest.Generic;
using MG.Sonarr.Functionality;
//using MG.Sonarr.Functionality.Extensions;
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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Cmdlets
{
    /// <summary>
    /// The main base <see cref="PSCmdlet"/> class for all PoshSonarr cmdlets.  Includes custom API methods along with advanced error-handling.
    /// </summary>
    public abstract class BaseSonarrCmdlet : PSCmdlet
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

        #region NEW API METHODS

        #region PROCESSORS
        private void ProcessVoidResponse(IRestResponse response)
        {
            if (response.IsFaulted)
            {
                if (response.HasException)
                    this.WriteError(response.GetAbsoluteException(), ErrorCategory.InvalidOperation);

                else if (!response.IsValidStatusCode)
                    this.WriteError(new NoSonarrResponseException(), ErrorCategory.ResourceUnavailable);
            }
            else
            {
                base.WriteDebug(string.Format("Received {0} ({1}) status code from the response.", response.StatusCode, response.StatusCode.ToString()));
                base.WriteVerbose(string.Format("Received {0} ({1}).", response.StatusCode, response.StatusCode.ToString()));
            }
        }
        private T ProcessSingularResponse<T>(IRestResponse<T> response)
            where T : class
        {
            if (!response.IsFaulted)
                return response.Content;

            else
            {
                if (response.HasException)
                    this.WriteError(response.GetAbsoluteException(), ErrorCategory.InvalidOperation);

                else if (!response.IsValidStatusCode)
                    this.WriteError(new NoSonarrResponseException(), ErrorCategory.ResourceUnavailable);

                else
                    this.WriteError(new HttpStatusException(response.StatusCode), ErrorCategory.InvalidResult);

                return default;
            }
        }
        private List<T> ProcessMultiResponse<T>(IRestListResponse<T> response)
            where T : class
        {
            if (!response.IsFaulted)
                return response.Content;

            else
            {
                if (response.HasException)
                    this.WriteError(response.GetAbsoluteException(), ErrorCategory.InvalidOperation);

                else if (!response.IsValidStatusCode)
                    this.WriteError(new NoSonarrResponseException(), ErrorCategory.ResourceUnavailable);

                else
                    this.WriteError(new HttpStatusException(response.StatusCode), ErrorCategory.InvalidResult);

                return default;
            }
        }

        #endregion

        #region GET
        /// <summary>
        /// Sends a GET request to the specified Sonarr endpoint, and processes the result into the specified class.
        /// </summary>
        /// <typeparam name="T">The class type to process the result content as.</typeparam>
        /// <param name="endpoint">The API endpoint that the request is sent to.</param>
        protected T SendSonarrGet<T>(string endpoint) where T : class
        {
            this.WriteApiDebug(endpoint, HttpMethod.Get, out string apiPath);
            IRestResponse<T> response = this.SendSonarrGetAsTask<T>(apiPath).GetAwaiter().GetResult();
            return this.ProcessSingularResponse(response);
        }

        /// <summary>
        /// Sends a GET request to the specified Sonarr endpoint, and returns the raw, unprocessed <see cref="string"/> content as a result.
        /// </summary>
        /// <param name="endpoint">The API endpoint that the request is sent to.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="HttpRequestException"/>
        protected string SendSonarrRawGet(string endpoint)
        {
            this.WriteApiDebug(endpoint, HttpMethod.Get, out string apiPath);
            if (string.IsNullOrWhiteSpace(apiPath))
                throw new ArgumentNullException("endpoint");

            IRestResponse response = this.SendSonarrRawGetAsync(apiPath).GetAwaiter().GetResult();
            if (!response.IsFaulted)
                return response.Content as string;

            else
            {
                if (response.HasException)
                    this.WriteError(response.GetAbsoluteException(), ErrorCategory.InvalidOperation);

                else if (!response.IsValidStatusCode)
                    this.WriteError(new NoSonarrResponseException(), ErrorCategory.ResourceUnavailable);

                else
                    this.WriteError(new HttpStatusException(response.StatusCode), ErrorCategory.InvalidResult);

                return default;
            }
        }
        private async Task<IRestResponse> SendSonarrRawGetAsync(string apiPath)
        {
            using (HttpResponseMessage response = await Context.ApiCaller.GetAsync(apiPath, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false))
            {
                IRestResponse rr = null;
                if (response.IsSuccessStatusCode)
                {
                    rr = new RestResponse(response);
                    using (var content = response.Content)
                    {
                        string strRes = await content.ReadAsStringAsync().ConfigureAwait(false);
                        rr.AddProcessedContent(strRes);
                    }
                }
                else
                    rr = new RestResponse(response);

                return rr;
            }
        }

        /// <summary>
        /// Sends a GET request to the specified Sonarr endpoint, and processes the response's content into a <see cref="List{T}"/> of the specified class.
        /// </summary>
        /// <typeparam name="T">The class type to process each result content as.</typeparam>
        /// <param name="endpoint">The API endpoint that the request is sent to.</param>
        protected List<T> SendSonarrListGet<T>(string endpoint) where T : class
        {
            this.WriteApiDebug(endpoint, HttpMethod.Get, out string apiPath);
            IRestListResponse<T> response = this.SendSonarrListGetAsTask<T>(apiPath).GetAwaiter().GetResult();
            return this.ProcessMultiResponse(response);
        }
        private Task<IRestResponse<T>> SendSonarrGetAsTask<T>(string apiPath) where T : class
        {
            return Context.ApiCaller.GetAsJsonAsync<T>(apiPath);
        }
        private Task<IRestListResponse<T>> SendSonarrListGetAsTask<T>(string apiPath) where T : class
        {
            return Context.ApiCaller.GetAsJsonListAsync<T>(apiPath);
        }

        #endregion

        #region POST
        /// <summary>
        /// Sends a POST request to the specified Sonarr endpoint, and processes the result into the specified class.
        /// </summary>
        /// <typeparam name="T">The class type to process the result content as.</typeparam>
        /// <param name="endpoint">The API endpoint that the request is sent to.</param>
        /// <param name="payload">A <see langword="class"/> object that is sent in the payload of the request.</param>
        protected T SendSonarrPost<T>(string endpoint, IJsonResult payload) where T : class
        {
            this.WriteApiDebug(endpoint, HttpMethod.Post, out string apiPath);
            IRestResponse<T> response = this.SendSonarrPostAsTask<T>(apiPath, payload).GetAwaiter().GetResult();
            return this.ProcessSingularResponse(response);
        }
        private Task<IRestResponse<T>> SendSonarrPostAsTask<T>(string apiPath, IJsonResult payload) where T : class
        {
            return Context.ApiCaller.PostAsJsonAsync<T>(new Uri(apiPath, UriKind.Relative), payload);
        }

        #endregion

        #region PUT
        /// <summary>
        /// Sends a PUT request to the specified Sonarr endpoint, and processes the result into the specified class.
        /// </summary>
        /// <typeparam name="T">The class type to process the result content as.</typeparam>
        /// <param name="endpoint">The API endpoint that the request is sent to.</param>
        /// <param name="payload">A <see langword="class"/> object that is sent in the payload of the request.</param>
        protected T SendSonarrPut<T>(string endpoint, IJsonResult payload) where T : class
        {
            this.WriteApiDebug(endpoint, HttpMethod.Put, out string apiPath);
            IRestResponse<T> response = this.SendSonarrPutAsTask<T>(apiPath, payload).GetAwaiter().GetResult();
            return this.ProcessSingularResponse(response);
        }
        private Task<IRestResponse<T>> SendSonarrPutAsTask<T>(string apiPath, IJsonResult payload) where T : class
        {
            return Context.ApiCaller.PutAsJsonAsync<T>(new Uri(apiPath, UriKind.Relative), payload);
        }

        #endregion

        #region DELETE
        /// <summary>
        /// Sends a PUT request to the specified Sonarr endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint that the request is sent to.</param>
        protected void SendSonarrDelete(string endpoint)
        {
            this.WriteApiDebug(endpoint, HttpMethod.Delete, out string apiPath);
            this.ProcessVoidResponse(this.SendSonarrDeleteAsTask(apiPath).GetAwaiter().GetResult());
        }
        private Task<IRestResponse> SendSonarrDeleteAsTask(string apiPath)
        {
            return Context.ApiCaller.DeleteAsJsonAsync(apiPath);
        }

        #endregion

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

        #region DEBUG METHODS
        /// <summary>
        /// Displays the raw JSON response received from the endpoint in the Debug and Verbose output streams.
        /// </summary>
        /// <param name="jsonResult">The JSON string from the response payload.</param>
        /// <param name="code">The status code from the <see cref="HttpResponseMessage"/>.</param>
        /// <param name="showAllDebug">Indicates whether to show the entire JSON response or to only show the status code.</param>
        protected void WriteApiDebug(string jsonResult, HttpStatusCode code, bool showAllDebug)
        {
            if (this.MyInvocation.BoundParameters.ContainsKey("Debug"))
            {
                string debugJson = null;
                if (showAllDebug && !string.IsNullOrWhiteSpace(jsonResult))
                {
                    JToken tok = JToken.Parse(jsonResult);
                    if (tok != null)
                    {
                        debugJson = tok.ToString();
                    }
                }
                base.WriteVerbose(string.Format("Received response: {0} ({1}", (int)code, code.ToString()));
                base.WriteDebug(string.Format(DEBUG_API_RESPONSE_MSG, (int)code, code.ToString(), Environment.NewLine, debugJson));
            }
        }
        /// <summary>
        /// Displays a non-bodied API-specific debug message if the DebugPreference is set to show Debug-level messages.
        /// It returns the "to-be-used" API uri string no matter what.
        /// </summary>
        /// <param name="endpoint">The endpoint Uri string that the <see cref="SonarrRestClient"/> will execute on.</param>
        /// <param name="method">The method that will be used in the API call.</param>
        /// <param name="apiPath">The parsed API uri to be executed on.</param>
        protected virtual void WriteApiDebug(string endpoint, HttpMethod method, out string apiPath)
        {
            apiPath = Context.SonarrUrl.Path + endpoint;

            string msg = string.Format(
                DEBUG_API_MSG,
                method.Method,
                Context.SonarrUrl.BaseUrl,
                apiPath
            );

            base.WriteDebug(msg);
        }

        /// <summary>
        /// Displays a bodied API-specific debug message if the DebugPreference is set to show Debug-level messages.
        /// It returns the "to-be-used" API uri string no matter what.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="method"></param>
        /// <param name="body"></param>
        /// <param name="apiPath"></param>
        protected virtual void WriteApiDebug(string endpoint, HttpMethod method, string body, out string apiPath)
        {
            apiPath = Context.SonarrUrl.Path + endpoint;

            string msg = string.Format(
                DEBUG_API_AND_BODY_MSG,
                method.Method,
                Context.SonarrUrl.BaseUrl,
                apiPath,
                Environment.NewLine,
                body
            );

            base.WriteDebug(msg);
        }

        #endregion

        #region EXCEPTION METHODS

        /// <summary>
        /// Takes in an <see cref="Exception"/> and returns the innermost <see cref="Exception"/> as a result.
        /// </summary>
        /// <param name="e">The exception to pull the innermost <see cref="Exception"/> from.</param>
        protected virtual Exception GetAbsoluteException(Exception e)
        {
            while (e.InnerException != null)
            {
                e = e.InnerException;
            }
            return e;
        }

        /// <summary>
        /// Issues a <see cref="PSCmdlet"/>.WriteError from a given string message and <see cref="ErrorCategory"/>.
        /// </summary>
        /// <param name="message">The exception message to be displayed in the <see cref="ErrorRecord"/>.</param>
        /// <param name="category">The category of the error.</param>
        protected void WriteError(string message, ErrorCategory category) =>
            this.WriteError(new ArgumentException(message), category, null);

        /// <summary>
        /// Issues a <see cref="PSCmdlet"/>.WriteError from a given string message, <see cref="ErrorCategory"/>, and Target Object.
        /// </summary>
        /// <param name="message">The exception message to be displayed in the <see cref="ErrorRecord"/>.</param>
        /// <param name="category">The category of the error.</param>
        /// <param name="targetObject">The object used as the 'targetObject' in an <see cref="ErrorRecord"/>.</param>
        protected void WriteError(string message, ErrorCategory category, object targetObject) =>
            this.WriteError(new ArgumentException(message), category, targetObject);

        /// <summary>
        /// /// Issues a <see cref="PSCmdlet"/>.WriteError from a given string message, base <see cref="Exception"/>, <see cref="ErrorCategory"/>, and Target Object.
        /// </summary>
        /// <param name="message">The exception message to be displayed in the <see cref="ErrorRecord"/>.</param>
        /// <param name="exception">The exception whose InnerException will be become the InnerException of the <see cref="ErrorRecord"/> and its type will be used as the FullyQualifiedErrorId.</param>
        /// <param name="category">The category of the error.</param>
        /// <param name="targetObject">The object used as the 'targetObject' in an <see cref="ErrorRecord"/>.</param>
        protected void WriteError(string message, Exception exception, ErrorCategory category, object targetObject)
        {
            exception = this.GetAbsoluteException(exception);

            var errRec = new ErrorRecord(new InvalidOperationException(message, exception), exception.GetType().FullName, category, targetObject);
            base.WriteError(errRec);
        }

        /// <summary>
        /// Issues a <see cref="PSCmdlet"/>.WriteError from a given base exception and <see cref="ErrorCategory"/>.
        /// </summary>
        /// <param name="baseException">The base exception will be become the InnerException of the <see cref="ErrorRecord"/> and its type will be used as the FullyQualifiedErrorId.</param>
        /// <param name="category"></param>
        protected void WriteError(Exception baseException, ErrorCategory category) => this.WriteError(baseException, category, null);

        /// <summary>
        /// Issues a <see cref="PSCmdlet"/>.WriteError from a base <see cref="Exception"/>, <see cref="ErrorCategory"/>, and Target Object.
        /// </summary>
        /// <param name="message">The exception message to be displayed in the <see cref="ErrorRecord"/>.</param>
        /// <param name="baseException">The base exception will be become the InnerException of the <see cref="ErrorRecord"/> and its type will be used as the FullyQualifiedErrorId.</param>
        /// <param name="category">The category of the error.</param>
        /// <param name="targetObject">The object used as the 'targetObject' in an <see cref="ErrorRecord"/>.</param>
        protected void WriteError(Exception baseException, ErrorCategory category, object targetObject)
        {
            var errRec = new ErrorRecord(baseException, baseException.GetType().FullName, category, targetObject);
            base.WriteError(errRec);
        }

        protected void WriteError(IRestResponseDetails details, object targetObj = null)
        {
            if (details.HasException)
            {
                this.WriteError(details.Exception, ErrorCategory.InvalidResult, targetObj);
            }
            else
            {
                this.WriteError(new ErrorRecord(new HttpRequestException(
                    string.Format("An unknown error occurred while sending the GET request - {0}", details.StatusCode.ToString())),
                    "MG.Sonarr.UnknownHttpException", ErrorCategory.InvalidOperation, targetObj));
            }
        }

        #endregion
    }
}