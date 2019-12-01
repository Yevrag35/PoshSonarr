using MG.Sonarr.Functionality.Extensions;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace MG.Sonarr.Functionality
{
    public static class ClassFactory
    {
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

        public static ISonarrUrl GenerateSonarrUrl(Uri url, bool includeApiPrefix) => new SonarrUrl(url, includeApiPrefix);
        public static ISonarrUrl GenerateSonarrUrl(string hostName, int portNumber, bool useSsl, string reverseProxyBase, bool includeApiPrefix)
        {
            return new SonarrUrl(hostName, portNumber, useSsl, reverseProxyBase, includeApiPrefix);
        }
        public static IEqualityComparer<string> NewIgnoreCase() => new IgnoreCase();
    }
}
