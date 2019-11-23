using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Extensions
{
    public static partial class HttpClientExtensions
    {
        public async static Task<T> PutAsJsonAsync<T>(this HttpClient httpClient, string uri, IJsonResult payload, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PutAsJsonAsync<T>(httpClient, new Uri(uri), payload, Encoding.UTF8, suppressExceptions);
        }
        public async static Task<T> PutAsJsonAsync<T>(this HttpClient httpClient, string uri, string stringPayload, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PutAsJsonAsync<T>(httpClient, new Uri(uri), stringPayload, Encoding.UTF8, suppressExceptions);
        }
        public async static Task<T> PutAsJsonAsync<T>(this HttpClient httpClient, string uri, IJsonResult payload, Encoding encoding, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PutAsJsonAsync<T>(httpClient, new Uri(uri), payload, encoding, suppressExceptions);
        }
        public async static Task<T> PutAsJsonAsync<T>(this HttpClient httpClient, string uri, string stringPayload, Encoding encoding, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PutAsJsonAsync<T>(httpClient, new Uri(uri), stringPayload, encoding, suppressExceptions);
        }
        public async static Task<T> PutAsJsonAsync<T>(this HttpClient httpClient, Uri uri, IJsonResult payload, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PutAsJsonAsync<T>(httpClient, uri, payload, Encoding.UTF8, suppressExceptions);
        }
        public async static Task<T> PutAsJsonAsync<T>(this HttpClient httpClient, Uri uri, string stringPayload, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PutAsJsonAsync<T>(httpClient, uri, stringPayload, Encoding.UTF8, suppressExceptions);
        }
        public async static Task<T> PutAsJsonAsync<T>(this HttpClient httpClient, Uri uri, IJsonResult payload, Encoding encoding, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PutAsJsonAsync<T>(httpClient, uri, payload.ToJson(), encoding, suppressExceptions);
        }
        public async static Task<T> PutAsJsonAsync<T>(this HttpClient httpClient, Uri uri, string stringPayload, Encoding encoding, bool suppressExceptions = false)
            where T : IJsonResult
        {
            using (var stringContent = new StringContent(stringPayload, encoding, CONTENT_TYPE))
            {
                using (HttpResponseMessage response = await httpClient.PutAsync(uri, stringContent))
                {
                    return await response.Content.ReadAsJsonAsync<T>(suppressExceptions);
                }
            }
        }
    }
}
