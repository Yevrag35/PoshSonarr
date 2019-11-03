using System;
using System.Collections.Generic;
using System.Net;

namespace MG.Sonarr.Functionality
{
    public interface IRestResponse<T>
    {
        Exception Exception { get; }
        bool IsFaulted { get; }
        T Result { get; }
        HttpStatusCode StatusCode { get; }

        Exception GetAbsoluteException();
    }
}
