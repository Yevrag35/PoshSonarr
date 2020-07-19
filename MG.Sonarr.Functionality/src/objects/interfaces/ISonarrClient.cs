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
        Task<IRestResponse<T>> GetAsJsonAsync<T>(string url) where T : class;
        Task<IRestListResponse<T>> GetAsJsonListAsync<T>(string url) where T : class;
    }
}
