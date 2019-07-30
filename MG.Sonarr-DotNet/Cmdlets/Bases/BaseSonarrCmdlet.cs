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
    /// <summary>
    /// The main base <see cref="PSCmdlet"/> class for all PoshSonarr cmdlets.  Includes custom API methods along with advanced error-handling.
    /// </summary>
    public abstract class BaseSonarrCmdlet : PSCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string API_PREFIX = "/api";
        private const string CONNECT_EP = "/system/status";
        private const string CONNECT_MSG = "Getting initial Sonarr status from {0}";
        private const string CONNECT_FORMAT = "{0}://{1}:{2}{3}";
        private const string CONTENT_TYPE = "application/json";

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            if (!Context.IsConnected)
                throw new SonarrContextNotSetException();
        }

        #endregion

        #region API METHODS

        /// <summary>
        /// Sends a GET request to the specified Sonarr endpoint and returning a JSON-formatted string in response.  This is for exclusive use with <see cref="ConnectInstance"/>.  
        /// Errors are handled by <see cref="PSCmdlet"/>.WriteError.
        /// </summary>
        /// <exception cref="Exception"/>
        protected string TrySonarrConnect()
        {
            string endpoint = Context.UriBase + CONNECT_EP;
            string url = string.Format(CONNECT_FORMAT,
                Context.ApiCaller.BaseAddress.Scheme,
                Context.ApiCaller.BaseAddress.Host,
                Context.ApiCaller.BaseAddress.Port,
                endpoint);

            string msg = string.Format(CONNECT_MSG, url);
            base.WriteDebug(msg);

            Task<HttpResponseMessage> task = Context.ApiCaller.GetAsync(endpoint, HttpCompletionOption.ResponseContentRead);
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
                    this.WriteError(new SonarrConnectException(endpoint, hre), ErrorCategory.ParserError);
                    return null;
                }
                catch (Exception e)
                {
                    throw this.GetAbsoluteException(e);
                }
            }

            return res;
        }

        /// <summary>
        /// Sends a DELETE request to the specified Sonarr endpoint.  Errors are handled by <see cref="PSCmdlet"/>.WriteError.
        /// </summary>
        /// <param name="endpoint">The Sonarr API endpoint to send the DELETE request to.</param>
        protected void TryDeleteSonarrResult(string endpoint)
        {
            endpoint = Context.UriBase + endpoint;
            base.WriteDebug(string.Format("DELETE REQUEST URL: {0}", endpoint));
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

        /// <summary>
        /// Sends a GET request to the specified Sonarr endpoint returning a JSON-formatted string in response.  
        /// Errors are handled by <see cref="PSCmdlet"/>.WriteError.
        /// </summary>
        /// <param name="endpoint">The Sonarr API endpoint to send the GET request to.</param>
        protected string TryGetSonarrResult(string endpoint)
        {
            endpoint = Context.UriBase + endpoint;
            base.WriteDebug(string.Format("GET REQUEST URL: {0}", endpoint));
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

        /// <summary>
        /// Sends a POST request to the specified Sonarr endpoint with the specified string payload formatted in JSON 
        /// returning a JSON-formatted string in response.  Errors are handled by <see cref="PSCmdlet"/>.WriteError.
        /// </summary>
        /// <param name="endpoint">The Sonarr API endpoint to send the POST request to.</param>
        /// <param name="jsonBody">The JSON payload content to be sent with the POST request.</param>
        protected string TryPostSonarrResult(string endpoint, string jsonBody)
        {
            endpoint = Context.UriBase + endpoint;
            base.WriteDebug(string.Format("POST REQUEST URL: {0}", endpoint));
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

        /// <summary>
        /// Sends a PUT request to the specified Sonarr endpoint with the specified string payload formatted in JSON 
        /// returning a JSON-formatted string in response.  Errors are handled by <see cref="PSCmdlet"/>.WriteError.
        /// </summary>
        /// <param name="endpoint">The Sonarr API endpoint to send the PUT request to.</param>
        /// <param name="jsonBody">The JSON payload content to be sent with the PUT request.</param>
        protected string TryPutSonarrResult(string endpoint, string jsonBody)
        {
            endpoint = Context.UriBase + endpoint;
            base.WriteDebug(string.Format("PUT REQUEST URL: {0}", endpoint));
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

        #endregion
    }
}