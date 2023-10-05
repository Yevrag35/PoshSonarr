using MG.Api.Json;
using MG.Api.Rest;
using MG.Api.Rest.Generic;
using MG.Sonarr.Functionality.Strings;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MG.Sonarr.Functionality.Client
{
    /// <summary>
    /// An interface whose exposed properties and methods are designed to send
    /// Sonarr-specific RESTful API requests.
    /// </summary>
    public interface ISonarrClient : IDisposable
    {
        /// <summary>
        /// Gets or sets the base address (URI) used when sending requests to the connected Sonarr instance.
        /// </summary>
        Uri BaseAddress { get; set; }

        /// <summary>
        /// Gets the headers which should be sent with each request.
        /// </summary>
        HttpRequestHeaders DefaultRequestHeaders { get; }

        /// <summary>
        /// Indicates that the <see cref="ISonarrClient"/> contains the proper authentication header and that <see cref="ISonarrClient.BaseAddress"/> 
        /// is not <see langword="null"/>.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Adds the specified <see cref="IApiKey"/> to the <see cref="HttpClient.DefaultRequestHeaders"/> collection.
        /// </summary>
        /// <param name="apiKey">The api key to be added to the <see cref="ISonarrClient.DefaultRequestHeaders"/> collection.</param>
        void AddApiKey(IApiKey apiKey);

        /// <summary>
        /// Sends an <see langword="async"/> DELETE request to the specified Sonarr endpoint.
        /// </summary>
        /// <param name="url">The Sonarr endpoint the request is sent to.</param>
        Task<IRestResponse> DeleteAsJsonAsync(string url);

        /// <summary>
        /// Sends an <see langword="async"/> GET request to the specified Sonarr endpoint and 
        /// deserializes the JSON-string response into the specified .NET type.
        /// </summary>
        /// <typeparam name="T">The .NET type the JSON response will be deserialized into.</typeparam>
        /// <param name="url">The Sonarr endpoint the request is sent to.</param>
        Task<IRestResponse<T>> GetAsJsonAsync<T>(string url) where T : class;
        /// <summary>
        /// Sends an <see langword="async"/> GET request to the specified Sonarr endpoint.  The JSON response
        /// is treated as an array and deserialized into a <see cref="List{T}"/>.
        /// </summary>
        /// <typeparam name="T">The .NET type of each <see cref="List{T}"/> member the JSON response will be deserialized into.</typeparam>
        /// <param name="url">The Sonarr endpoint the request is sent to.</param>
        Task<IRestListResponse<T>> GetAsJsonListAsync<T>(string url) where T : class;
        /// <summary>
        /// Sends an <see langword="async"/> GET request to the specified Sonarr endpoint with an HTTP completion option.
        /// </summary>
        /// <param name="url">The Sonarr endpoint the request is sent to.</param>
        /// <param name="completionOption">
        ///     An HTTP completion option value that indicates when the operation should
        ///     be considered complete.
        /// </param>
        Task<HttpResponseMessage> GetAsync(string url, HttpCompletionOption completionOption);

        /// <summary>
        /// Sends an <see langword="async"/> POST request with no payload in the request body to
        /// the specified Sonarr endpoint.
        /// </summary>
        /// <param name="url">The Sonarr endpoint the request is sent to.</param>
        Task<IRestResponse<string>> PostAsJsonAsync(string url);
        /// <summary>
        /// Sends an <see langword="async"/> POST request with the specified serializable <see cref="IJsonObject"/> payload
        /// in the request body to the specified Sonarr endpoint.
        /// </summary>
        /// <typeparam name="T">The .NET type the JSON response will be deserialized into.</typeparam>
        /// <param name="url">The Sonarr endpoint the request is sent to.</param>
        /// <param name="payload">A serializable JSON payload to be sent in the body of the request.</param>
        Task<IRestResponse<T>> PostAsJsonAsync<T>(string url, IJsonObject payload) where T : class;
        /// <summary>
        /// Sends an <see langword="async"/> POST request with the specified serializable <see cref="IJsonObject"/> payload
        /// in the request body to the specified Sonarr endpoint.  The JSON response is then treated as an array
        /// and deserialized into a <see cref="List{T}"/>.
        /// </summary>
        /// <typeparam name="T">The .NET type of each <see cref="List{T}"/> member the JSON response will be deserialized into.</typeparam>
        /// <param name="url">The Sonarr endpoint the request is sent to.</param>
        /// <param name="payload">A serializable JSON payload to be sent in the body of the request.</param>
        Task<IRestListResponse<T>> PostAsJsonListAsync<T>(string url, IJsonObject payload) where T : class;

        /// <summary>
        /// Sends an <see langword="async"/> PUT request with the specified serializable <see cref="IJsonObject"/> payload
        /// in the request body to the specified Sonarr endpoint.
        /// </summary>
        /// <typeparam name="T">The .NET type the JSON response will be deserialized into.</typeparam>
        /// <param name="url">The Sonarr endpoint the request is sent to.</param>
        /// <param name="payload">A serializable JSON payload to be sent in the body of the request.</param>
        Task<IRestResponse<T>> PutAsJsonAsync<T>(string url, IJsonObject payload) where T : class;
        /// <summary>
        /// Sends an <see langword="async"/> PUT request with the specified serializable <see cref="IJsonObject"/> payload
        /// in the request body to the specified Sonarr endpoint.  The JSON response will be deserialized according to
        /// <paramref name="type"/> and returned as a non-generic <see cref="object"/>.
        /// </summary>
        /// <typeparam name="T">The .NET type the JSON response will be deserialized into.</typeparam>
        /// <param name="url">The Sonarr endpoint the request is sent to.</param>
        /// <param name="payload">A serializable JSON payload to be sent in the body of the request.</param>
        /// <param name="type">The .NET type the JSON response will be deserialized into.</param>
        Task<IRestResponse> PutAsObjectAsync(string url, IJsonObject payload, Type type);
    }
}
