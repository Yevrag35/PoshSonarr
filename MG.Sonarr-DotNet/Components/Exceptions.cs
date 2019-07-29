using System;
using System.Net;
using System.Net.Http;

namespace MG.Sonarr
{
    public abstract class BaseSonarrHttpException : HttpRequestException
    {
        private const string MSG = "An exception occurred while sending a {0} request to Sonarr.";

        public ApiCaller Caller { get; }
        public string RequestMethod { get; }
        public Uri Url { get; protected set; }

        public BaseSonarrHttpException(string method)
            : base(string.Format(MSG, method))
        {
        }

        public BaseSonarrHttpException(string method, ApiCaller caller)
            : this(method) => this.Caller = caller;
    }

    public class InvalidApiKeyException : ArgumentException
    {
        private const string EX_MSG = "The input key specified is not of the valid format. (32 characters; all lowercase).";
        public InvalidApiKeyException()
            : base(EX_MSG) { }
    }

    public class NoSonarrResponseException : InvalidOperationException
    {
        private const string MSG = "No response was received from the Sonarr server.";

        public ApiCaller Caller { get; }
        public NoSonarrResponseException(ApiCaller caller)
            : base(MSG) => this.Caller = caller;
    }

    public class SonarrGetRequestException : BaseSonarrHttpException
    {
        public SonarrGetRequestException(ApiCaller caller)
            : base(HttpMethod.Get.Method, caller)
        {
            //this.Url = !Context.NoApiPrefix
            //    ? 
        }
        public SonarrGetRequestException(HttpRequestMessage msg)
            : base(HttpMethod.Get.Method) => this.Url = msg.RequestUri;
    }
}
