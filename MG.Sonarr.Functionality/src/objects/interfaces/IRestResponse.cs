using System;
using System.Net;

namespace MG.Sonarr.Functionality
{
    public interface IRestResponse
    {
        Exception Exception { get; }
        bool HasException { get; }
        bool IsFaulted { get; }
        HttpStatusCode StatusCode { get; }

        Exception GetAbsoluteException();
    }
}
