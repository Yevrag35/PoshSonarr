using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Functionality.Extensions
{
    public static partial class HttpClientExtensions
    {
        /// <summary>
        /// Sends a PUT request with the supplied <see cref="IJsonResult"/> payload, which is encoded using the default encoding (<see cref="Encoding.UTF8"/>), to the designated endpoint.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The URI, in <see cref="string"/> form, the POST request is sent to.</param>
        /// <param name="payload">A <see cref="IJsonResult"/> object that is sent in the payload of the request.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="ArgumentNullException">The specified payload is null.</exception>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<IRestResponse<T>> PutAsJsonAsync<T>(this HttpClient httpClient, string uri, IJsonResult payload, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PutAsJsonAsync<T>(httpClient, new Uri(uri), payload, Encoding.UTF8, suppressExceptions).ConfigureAwait(false);
        }
        /// <summary>
        /// Sends a PUT request with the supplied <see cref="string"/> payload, which is encoded using the default encoding (<see cref="Encoding.UTF8"/>), to the designated endpoint.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The URI, in <see cref="string"/> form, the POST request is sent to.</param>
        /// <param name="stringPayload">The <see cref="string"/> that is sent in the payload of the request.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="ArgumentNullException">The specified payload is null.</exception>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<IRestResponse<T>> PutAsJsonAsync<T>(this HttpClient httpClient, string uri, string stringPayload, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PutAsJsonAsync<T>(httpClient, new Uri(uri), stringPayload, Encoding.UTF8, suppressExceptions).ConfigureAwait(false);
        }
        /// <summary>
        /// Sends a PUT request with the supplied <see cref="IJsonResult"/> payload, which is encoded using the specified <see cref="Encoding"/>, to the designated endpoint.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The URI, in <see cref="string"/> form, the POST request is sent to.</param>
        /// <param name="payload">A <see cref="IJsonResult"/> object that is sent in the payload of the request.</param>
        /// <param name="encoding">The <see cref="Encoding"/> the specified payload is encoded in.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="ArgumentNullException">The specified payload is null.</exception>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<IRestResponse<T>> PutAsJsonAsync<T>(this HttpClient httpClient, string uri, IJsonResult payload, Encoding encoding, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PutAsJsonAsync<T>(httpClient, new Uri(uri), payload, encoding, suppressExceptions).ConfigureAwait(false);
        }
        /// <summary>
        /// Sends a PUT request with the supplied <see cref="string"/> payload, which is encoded using the specified <see cref="Encoding"/>, to the designated endpoint.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The URI, in <see cref="string"/> form, the POST request is sent to.</param>
        /// <param name="stringPayload">The <see cref="string"/> that is sent in the payload of the request.</param>
        /// <param name="encoding">The <see cref="Encoding"/> the specified payload is encoded in.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="ArgumentNullException">The specified payload is null.</exception>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<IRestResponse<T>> PutAsJsonAsync<T>(this HttpClient httpClient, string uri, string stringPayload, Encoding encoding, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PutAsJsonAsync<T>(httpClient, new Uri(uri), stringPayload, encoding, suppressExceptions).ConfigureAwait(false);
        }
        /// <summary>
        /// Sends a PUT request with the supplied <see cref="IJsonResult"/> payload, which is encoded using the default encoding (<see cref="Encoding.UTF8"/>), to the designated endpoint.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The <see cref="Uri"/> the POST request is sent to.</param>
        /// <param name="payload">A <see cref="IJsonResult"/> object that is sent in the payload of the request.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="ArgumentNullException">The specified payload is null.</exception>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<IRestResponse<T>> PutAsJsonAsync<T>(this HttpClient httpClient, Uri uri, IJsonResult payload, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PutAsJsonAsync<T>(httpClient, uri, payload, Encoding.UTF8, suppressExceptions).ConfigureAwait(false);
        }
        /// <summary>
        /// Sends a PUT request with the supplied <see cref="string"/> payload, which is encoded using the default encoding (<see cref="Encoding.UTF8"/>), to the designated endpoint.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The <see cref="Uri"/> the POST request is sent to.</param>
        /// <param name="stringPayload">The <see cref="string"/> that is sent in the payload of the request.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="ArgumentNullException">The specified payload is null.</exception>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<IRestResponse<T>> PutAsJsonAsync<T>(this HttpClient httpClient, Uri uri, string stringPayload, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PutAsJsonAsync<T>(httpClient, uri, stringPayload, Encoding.UTF8, suppressExceptions).ConfigureAwait(false);
        }
        /// <summary>
        /// Sends a PUT request with the supplied <see cref="IJsonResult"/> payload, which is encoded using the specified <see cref="Encoding"/>, to the designated endpoint.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The <see cref="Uri"/> the POST request is sent to.</param>
        /// <param name="payload">A <see cref="IJsonResult"/> object that is sent in the payload of the request.</param>
        /// <param name="encoding">The <see cref="Encoding"/> the specified payload is encoded in.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="ArgumentNullException">The specified payload is null.</exception>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<IRestResponse<T>> PutAsJsonAsync<T>(this HttpClient httpClient, Uri uri, IJsonResult payload, Encoding encoding, bool suppressExceptions = false)
            where T : IJsonResult
        {
            return await PutAsJsonAsync<T>(httpClient, uri, payload.ToJson(), encoding, suppressExceptions).ConfigureAwait(false);
        }
        /// <summary>
        /// Sends a PUT request with the supplied <see cref="string"/> payload, which is encoded using the specified <see cref="Encoding"/>, to the designated endpoint.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance this method is extending.</param>
        /// <param name="uri">The <see cref="Uri"/> the POST request is sent to.</param>
        /// <param name="stringPayload">The <see cref="string"/> that is sent in the payload of the request.</param>
        /// <param name="encoding">The <see cref="Encoding"/> the specified payload is encoded in.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <exception cref="ArgumentNullException">The specified payload is null.</exception>
        /// <exception cref="JsonSerializationException"/>
        public async static Task<IRestResponse<T>> PutAsJsonAsync<T>(this HttpClient httpClient, Uri uri, string stringPayload, Encoding encoding, bool suppressExceptions = false)
            where T : IJsonResult
        {
            if (stringPayload == null)
                throw new ArgumentNullException("The specified payload is null.");

            using (var stringContent = new StringContent(stringPayload, encoding, CONTENT_TYPE))
            {
                using (HttpResponseMessage response = await httpClient.PutAsync(uri, stringContent).ConfigureAwait(false))
                {
                    return await ClassFactory.GenerateSingleResponse<T>(response, suppressExceptions).ConfigureAwait(false);
                }
            }
        }
    }
}
