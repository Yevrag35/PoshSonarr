using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MG.Sonarr.Functionality.Extensions
{
    /// <summary>
    /// Extensions for JSON-specific calls through <see cref="HttpClient"/>.
    /// </summary>
    public static partial class HttpClientExtensions
    {
        private const string CONTENT_TYPE = "application/json";
        public async static Task<IRestResponse<T>> GenerateSingleResponse<T>(HttpResponseMessage response, bool suppressExceptions = false)
            where T : IJsonResult
        {
            var restResponse = new SonarrRestResponse<T>(response.StatusCode, response.IsSuccessStatusCode);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    restResponse.Result = await response.Content.ReadAsJsonAsync<T>(suppressExceptions).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    restResponse.Exception = e;
                }
            }
            else
            {
                restResponse.Exception = new HttpRequestException(response.ReasonPhrase);
            }
            return restResponse;
        }
        public async static Task<IRestResponse<List<T>>> GenerateMultipleResponse<T>(HttpResponseMessage response, bool suppressExceptions = false)
            where T : IJsonResult
        {
            var restResponse = new SonarrRestResponse<List<T>>(response.StatusCode, response.IsSuccessStatusCode);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    restResponse.Result = await response.Content.ReadAsJsonAsync<List<T>>(suppressExceptions).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    restResponse.Exception = e;
                }
            }
            else
            {
                restResponse.Exception = new HttpRequestException(response.ReasonPhrase);
            }
            return restResponse;
        }
    }
}
