using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MG.Sonarr.Functionality.Extensions
{
    public static partial class HttpClientExtensions
    {
        public async static Task<IRestResponse<T>> GetAsJsonAsync<T>(this HttpClient httpClient, Uri uri, bool suppressExceptions = false)
            where T : IJsonResult
        {
            using (HttpResponseMessage response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false))
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
        }

        public async static Task<IRestResponse<List<T>>> GetAsJsonListAsync<T>(this HttpClient httpClient, Uri uri, bool suppressExceptions = false)
            where T : IJsonResult
        {
            using (HttpResponseMessage response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false))
            {
                var restResponse = new SonarrRestResponse<List<T>>(response.StatusCode, response.IsSuccessStatusCode);
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        restResponse.Result = await response.Content.ReadAsJsonListAsync<T>(suppressExceptions).ConfigureAwait(false);
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
}
