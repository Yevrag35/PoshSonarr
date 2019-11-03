using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace MG.Sonarr.Functionality
{
    internal class SonarrRestResponse<T> : IRestResponse<T>
    {
        private static HttpStatusCode[] BadStatues => new HttpStatusCode[] { HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.BadGateway, HttpStatusCode.Conflict, HttpStatusCode.GatewayTimeout, HttpStatusCode.Gone };

        internal Exception Exception { get; set; }
        internal HttpStatusCode StatusCode { get; set; }
        internal T Result { get; set; }

        Exception IRestResponse<T>.Exception => this.Exception;
        bool IRestResponse<T>.IsFaulted => this.Exception != null || BadStatues.Contains(this.StatusCode);
        HttpStatusCode IRestResponse<T>.StatusCode => this.StatusCode;
        T IRestResponse<T>.Result => this.Result;

        internal SonarrRestResponse() { }
        internal SonarrRestResponse(HttpResponseMessage response, T content)
        {
            this.Result = content;
            this.StatusCode = response.StatusCode;
        }

        Exception IRestResponse<T>.GetAbsoluteException()
        {
            Exception e = this.Exception;
            if (e == null)
                return null;

            else
            {
                while (e.InnerException != null)
                {
                    e = e.InnerException;
                }
                return e;
            }
        }
    }
}
