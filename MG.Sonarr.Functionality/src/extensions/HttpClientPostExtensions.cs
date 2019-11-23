using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Functionality.Extensions
{
    public static partial class HttpClientExtensions
    {

        #region POST-NO-BODY-AS-JSON ASYNC
        /// <summary>
        /// Sends a POST request with no payload to the designated endpoint.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The URI, in <see cref="string"/> form, the POST request is sent to.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<T> PostAsJsonAsync<T>(this HttpClient httpClient, string uri, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PostAsJsonAsync<T>(httpClient, new Uri(uri), suppressExceptions);
        }
        /// <summary>
        /// Sends a POST request with no payload to the designated endpoint.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The <see cref="Uri"/> the POST request is sent to.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<T> PostAsJsonAsync<T>(this HttpClient httpClient, Uri uri, bool suppressExceptions = false)
            where T : IJsonResult
        {
            using (HttpResponseMessage response = await httpClient.PostAsync(uri, null))
            {
                return await response.Content.ReadAsJsonAsync<T>(suppressExceptions);
            }
        }

        #endregion

        #region POST-AS-JSON ASYNC
        /// <summary>
        /// Sends a POST request with the supplied <see cref="IJsonResult"/> payload, which will be encoded with the default encoding (<see cref="Encoding.UTF8"/>), to the designated endpoint. 
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The URI, in <see cref="string"/> form, the POST request is sent to.</param>
        /// <param name="payload">A <see cref="IJsonResult"/> object that is sent in the payload of the request.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="ArgumentNullException">The specified payload object is null.</exception>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<T> PostAsJsonAsync<T>(this HttpClient httpClient, string uri, IJsonResult payload, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PostAsJsonAsync<T>(httpClient, new Uri(uri), payload, Encoding.UTF8, suppressExceptions);
        }
        /// <summary>
        /// Sends a POST request with the supplied <see cref="string"/> payload, which will be encoded with the default encoding (<see cref="Encoding.UTF8"/>), to the designated endpoint. 
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The URI, in <see cref="string"/> form, the POST request is sent to.</param>
        /// <param name="stringPayload">The <see cref="string"/> that is sent in the payload of the request.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<T> PostAsJsonAsync<T>(this HttpClient httpClient, string uri, string stringPayload, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PostAsJsonAsync<T>(httpClient, new Uri(uri), stringPayload, Encoding.UTF8, suppressExceptions);
        }
        /// <summary>
        /// Sends a POST request with the supplied <see cref="IJsonResult"/> payload, which will be encoded with the specified <see cref="Encoding"/>, to the designated endpoint.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The URI, in <see cref="string"/> form, the POST request is sent to.</param>
        /// <param name="payload">A <see cref="IJsonResult"/> object that is sent in the payload of the request.</param>
        /// <param name="encoding">The <see cref="Encoding"/> the specified payload is encoded in.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="ArgumentNullException">The specified payload object is null.</exception>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<T> PostAsJsonAsync<T>(this HttpClient httpClient, string uri, IJsonResult payload, Encoding encoding, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PostAsJsonAsync<T>(httpClient, new Uri(uri), payload, encoding, suppressExceptions);
        }
        /// <summary>
        /// Sends a POST request with the supplied <see cref="string"/> payload, which will be encoded with the specified <see cref="Encoding"/>, to the designated endpoint.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The URI, in <see cref="string"/> form, the POST request is sent to.</param>
        /// <param name="stringPayload">The <see cref="string"/> that is sent in the payload of the request.</param>
        /// <param name="encoding">The <see cref="Encoding"/> the specified payload is encoded in.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<T> PostAsJsonAsync<T>(this HttpClient httpClient, string uri, string stringPayload, Encoding encoding, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PostAsJsonAsync<T>(httpClient, new Uri(uri), stringPayload, encoding, suppressExceptions);
        }
        /// <summary>
        /// Sends a POST request with the supplied <see cref="IJsonResult"/> payload, which will be encoded with the default encoding (<see cref="Encoding.UTF8"/>), to the designated endpoint. 
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The <see cref="Uri"/> the POST request is sent to.</param>
        /// <param name="payload">A <see cref="IJsonResult"/> object that is sent in the payload of the request.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="ArgumentNullException">The specified payload object is null.</exception>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<T> PostAsJsonAsync<T>(this HttpClient httpClient, Uri uri, IJsonResult payload, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PostAsJsonAsync<T>(httpClient, uri, payload, Encoding.UTF8, suppressExceptions);
        }
        /// <summary>
        /// Sends a POST request with the supplied <see cref="string"/> payload, which will be encoded with the default encoding (<see cref="Encoding.UTF8"/>), to the designated endpoint. 
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The <see cref="Uri"/> the POST request is sent to.</param>
        /// <param name="stringPayload">The <see cref="string"/> that is sent in the payload of the request.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<T> PostAsJsonAsync<T>(this HttpClient httpClient, Uri uri, string stringPayload, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PostAsJsonAsync<T>(httpClient, uri, stringPayload, Encoding.UTF8, suppressExceptions);
        }
        /// <summary>
        /// Sends a POST request with the supplied <see cref="IJsonResult"/> payload, which will be encoded with the specified <see cref="Encoding"/>, to the designated endpoint.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The <see cref="Uri"/> the POST request is sent to.</param>
        /// <param name="payload">A <see cref="IJsonResult"/> object that is sent in the payload of the request.</param>
        /// <param name="encoding">The <see cref="Encoding"/> the specified payload is encoded in.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="ArgumentNullException">The specified payload object is null.</exception>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<T> PostAsJsonAsync<T>(this HttpClient httpClient, Uri uri, IJsonResult payload, Encoding encoding, bool suppressExceptions = false)
            where T : IJsonResult
        {
            if (payload == null)
                throw new ArgumentNullException("The specified payload object is null.");

            else
                return await PostAsJsonAsync<T>(httpClient, uri, payload.ToJson(), encoding, suppressExceptions);
        }
        /// <summary>
        /// Sends a POST request with the supplied <see cref="string"/> payload, which will be encoded with the specified <see cref="Encoding"/>, to the designated endpoint.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The <see cref="Uri"/> the POST request is sent to.</param>
        /// <param name="stringPayload">The <see cref="string"/> that is sent in the payload of the request.</param>
        /// <param name="encoding">The <see cref="Encoding"/> the specified payload is encoded in.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<T> PostAsJsonAsync<T>(this HttpClient httpClient, Uri uri, string stringPayload, Encoding encoding, bool suppressExceptions = false)
            where T : IJsonResult
        {
            if (string.IsNullOrEmpty(stringPayload))
                return await PostAsJsonAsync<T>(httpClient, uri, suppressExceptions);

            else
            {
                using (var stringContent = new StringContent(stringPayload, encoding, CONTENT_TYPE))
                {
                    using (HttpResponseMessage response = await httpClient.PostAsync(uri, stringContent))
                    {
                        return await response.Content.ReadAsJsonAsync<T>(suppressExceptions);
                    }
                }
            }
        }

        #endregion
    }
}
