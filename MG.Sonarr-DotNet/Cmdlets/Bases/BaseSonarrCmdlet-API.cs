﻿using MG.Api.Json;
using MG.Api.Rest;
using MG.Api.Rest.Generic;
using MG.Sonarr.Functionality.Exceptions;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net.Http;
using System.Threading.Tasks;

namespace MG.Sonarr.Cmdlets
{
    public abstract partial class BaseSonarrCmdlet
    {
        #region NEW API METHODS

        #region PROCESSORS
        private void ProcessVoidResponse<T>(IRestResponse<T> response) where T : class
        {
            if (response.IsFaulted)
            {
                if (response.HasException)
                    this.WriteError(response.Exception, ErrorCategory.InvalidOperation);

                else if (!response.IsValidStatusCode)
                    this.WriteError(new NoSonarrResponseException(), ErrorCategory.ResourceUnavailable);
            }
            else
            {
                base.WriteDebug(string.Format("Received {0} ({1}) status code from the response.", response.StatusCode, response.StatusCode.ToString()));
                base.WriteVerbose(string.Format("Received {0} ({1}).", response.StatusCode, response.StatusCode.ToString()));
            }
        }
        private void ProcessVoidResponse(IRestResponse response)
        {
            if (response.IsFaulted)
            {
                if (response.HasException)
                    this.WriteError(response.Exception, ErrorCategory.InvalidOperation);

                else if (!response.IsValidStatusCode)
                    this.WriteError(new NoSonarrResponseException(), ErrorCategory.ResourceUnavailable);
            }
            else
            {
                base.WriteDebug(string.Format("Received {0} ({1}) status code from the response.", response.StatusCode, response.StatusCode.ToString()));
                base.WriteVerbose(string.Format("Received {0} ({1}).", response.StatusCode, response.StatusCode.ToString()));
            }
        }
        private object ProcessSingularRepsonse(IRestResponse response)
        {
            this.ProcessVoidResponse(response);
            return response.Content;
        }
        private T ProcessSingularResponse<T>(IRestResponse<T> response)
            where T : class
        {
            if (!response.IsFaulted)
                return response.Content;

            else
            {
                if (response.HasException && response.HasRawContent)
                {
                    if (!response.ContentAsRaw.StartsWith("["))
                    {
                        this.WriteError(response.ContentAsJObject().ToString(), ErrorCategory.InvalidResult);
                    }
                    else
                    {
                        foreach (ErrorResultException ere in response.ContentAsJArray().ToObject<ErrorResultCollection>())
                        {
                            this.WriteError(ere, ErrorCategory.SyntaxError);
                        }
                    }
                }

                else if (response.HasException && !response.HasRawContent)
                    this.WriteError(response.Exception, ErrorCategory.InvalidOperation);

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
            {
                Type genComparable = typeof(IComparable<T>);
                if (typeof(T).GetInterfaces().Contains(genComparable))
                    response.Content.Sort();

                return response.Content;
            }
            else
            {
                if (response.HasException)
                    this.WriteError(response.Exception, ErrorCategory.InvalidOperation);

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
        protected T SendSonarrPost<T>(string endpoint, IJsonObject payload) where T : class
        {
            this.WriteApiDebug(endpoint, HttpMethod.Post, out string apiPath);
            IRestResponse<T> response = this.SendSonarrPostAsTask<T>(apiPath, payload).GetAwaiter().GetResult();
            return this.ProcessSingularResponse(response);
        }
        
        private Task<IRestResponse<T>> SendSonarrPostAsTask<T>(string apiPath, IJsonObject payload) where T : class
        {
            return Context.ApiCaller.PostAsJsonAsync<T>(apiPath, payload);
        }
        
        protected void SendSonarrPostNoData(string endpoint)
        {
            this.WriteApiDebug(endpoint, HttpMethod.Post, out string apiPath);
            IRestResponse<string> response = this.SendSonarrPostNoDataAsTask(apiPath).GetAwaiter().GetResult();
            this.ProcessVoidResponse(response);

        }
        private Task<IRestResponse<string>> SendSonarrPostNoDataAsTask(string apiPath)
        {
            return Context.ApiCaller.PostAsJsonAsync(apiPath);
        }

        protected List<T> SendSonarrListPost<T>(string endpoint, IJsonObject payload) where T : class
        {
            this.WriteApiDebug(endpoint, HttpMethod.Post, out string apiPath);
            IRestListResponse<T> response = this.SendSonarrListPostAsTask<T>(apiPath, payload).GetAwaiter().GetResult();
            return this.ProcessMultiResponse(response);

        }
        private Task<IRestListResponse<T>> SendSonarrListPostAsTask<T>(string apiPath, IJsonObject payload) where T : class
        {
            return Context.ApiCaller.PostAsJsonListAsync<T>(apiPath, payload);
        }

        #endregion

        #region PUT
        /// <summary>
        /// Sends a PUT request to the specified Sonarr endpoint, and processes the result into the specified class.
        /// </summary>
        /// <typeparam name="T">The class type to process the result content as.</typeparam>
        /// <param name="endpoint">The API endpoint that the request is sent to.</param>
        /// <param name="payload">A <see langword="class"/> object that is sent in the payload of the request.</param>
        protected T SendSonarrPut<T>(string endpoint, IJsonObject payload) where T : class
        {
            this.WriteApiDebug(endpoint, HttpMethod.Put, out string apiPath);
            IRestResponse<T> response = this.SendSonarrPutAsTask<T>(apiPath, payload).GetAwaiter().GetResult();
            return this.ProcessSingularResponse(response);
        }
        protected object SendSonarrPut(string endpoint, IJsonObject payload, Type objectType)
        {
            this.WriteApiDebug(endpoint, HttpMethod.Put, out string apiPath);
            IRestResponse response = this.SendSonarrPutAsTask(apiPath, payload, objectType).GetAwaiter().GetResult();
            return this.ProcessSingularRepsonse(response);

        }

        private Task<IRestResponse<T>> SendSonarrPutAsTask<T>(string apiPath, IJsonObject payload) where T : class
        {
            return Context.ApiCaller.PutAsJsonAsync<T>(apiPath, payload);
        }
        private Task<IRestResponse> SendSonarrPutAsTask(string apiPath, IJsonObject payload, Type payloadType)
        {
            return Context.ApiCaller.PutAsObjectAsync(apiPath, payload, payloadType);
        }

        #endregion

        #region DELETE
        /// <summary>
        /// Sends a DELETE request to the specified Sonarr endpoint.
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
    }
}
