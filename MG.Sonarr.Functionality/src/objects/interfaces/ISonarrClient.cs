using MG.Api.Rest;
using MG.Api.Rest.Generic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MG.Sonarr.Functionality
{
    public interface ISonarrClient
    {
        bool IsAuthenticated { get; }

        void AddApiKey(IApiKey apiKey);
        bool IsJsonArray(string jsonString);

        Task<IRestResponse> DeleteAsJsonAsync(string url);
        Task<IRestResponse<T>> GetAsJsonAsync<T>(string url) where T : class;
        Task<IRestListResponse<T>> GetAsJsonListAsync<T>(string url) where T : class;
        Task<IRestResponse<T>> PostAsJsonAsync<T>(string url, IJsonResult payload) where T : class;
        Task<IRestListResponse<T>> PostAsJsonListAsync<T>(string url, IJsonResult payload) where T : class;
        Task<IRestResponse<T>> PutAsJsonAsync<T>(string url, IJsonResult payload) where T : class;
        Task<IRestResponse> PutAsObjectAsync(string url, IJsonResult payload, Type type);
    }
}
