using System;
using System.Net;
using System.Net.Http;

namespace MG.Sonarr.Functionality.Exceptions
{
    /// <summary>
    /// An exception that is thrown when a <see cref="HttpRequestException"/> is triggered with a specific <see cref="HttpStatusCode"/>
    /// but no more details about the exception were provided from the remote server.
    /// </summary>
    public class HttpStatusException : HttpRequestException
    {
        private const string DEF_MSG = "An error occurred while sending the request.  HttpStatusCode: {0} - \"{1}\"";

        public HttpStatusException(HttpStatusCode statusCode)
            : base(string.Format(DEF_MSG, statusCode, statusCode.ToString()))
        {
        }
    }
}
