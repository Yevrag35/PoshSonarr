using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace MG.Sonarr.Functionality
{
    internal class SonarrRestResponse<T> : IRestResponse<T>
    {
        private bool _isFaulted = false;

        internal Exception Exception { get; set; }
        internal HttpStatusCode StatusCode { get; set; }
        internal T Result { get; set; }

        Exception IRestResponse.Exception => this.Exception;
        bool IRestResponse.HasException => this.Exception != null;
        bool IRestResponse.IsFaulted => _isFaulted || this.Exception != null;
        HttpStatusCode IRestResponse.StatusCode => this.StatusCode;
        T IRestResponse<T>.Result => this.Result;

        //internal SonarrRestResponse() { }
        internal SonarrRestResponse(HttpStatusCode statusCode, bool isSuccessCode)
        {
            _isFaulted = !isSuccessCode;
            this.StatusCode = statusCode;
        }
        internal SonarrRestResponse(HttpStatusCode statusCode, bool isSuccessCode, T content)
        {
            _isFaulted = !isSuccessCode;
            this.StatusCode = statusCode;
            this.Result = content;
        }
        internal SonarrRestResponse(HttpResponseMessage response, T content)
        {
            this.Result = content;
            this.StatusCode = response.StatusCode;
            _isFaulted = !response.IsSuccessStatusCode;
        }

        Exception IRestResponse.GetAbsoluteException()
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
