using System;
using System.Net;

namespace MG.Sonarr.Functionality
{
    public interface IRestResponse<T> : IRestResponse
    {
        T Result { get; }
    }
}
