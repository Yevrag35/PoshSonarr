using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MG.Sonarr.Functionality.Extensions
{
    public static partial class HttpClientExtensions
    {
        public async static Task<IRestResponse<T>> GetAsJsonAsync<T>(this HttpClient httpClient, string uri, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await GetAsJsonAsync<T>(httpClient, new Uri(uri, UriKind.Relative), suppressExceptions).ConfigureAwait(false);
        }

        public async static Task<IRestResponse<T>> GetAsJsonAsync<T>(this HttpClient httpClient, Uri uri, bool suppressExceptions = false)
            where T : IJsonResult
        {
            using (HttpResponseMessage response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false))
            {
                return await ClassFactory.GenerateSingleResponse<T>(response, suppressExceptions).ConfigureAwait(false);
            }
        }

        public async static Task<IRestResponse<List<T>>> GetAsJsonListAsync<T>(this HttpClient httpClient, string uri, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await GetAsJsonListAsync<T>(httpClient, new Uri(uri, UriKind.Relative), suppressExceptions).ConfigureAwait(false);
        }
        public async static Task<IRestResponse<List<T>>> GetAsJsonListAsync<T>(this HttpClient httpClient, Uri uri, bool suppressExceptions = false)
            where T : IJsonResult
        {
            using (HttpResponseMessage response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false))
            {
                return await ClassFactory.GenerateMultipleResponse<T>(response, suppressExceptions).ConfigureAwait(false);
            }
        }
    }
}
