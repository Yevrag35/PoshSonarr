using System;
using System.Net;
using System.Net.Http;

namespace MG.Sonarr
{
    public abstract class BaseSonarrHttpException : HttpRequestException
    {
        internal const string CAUTION = "Allowing redirects ";
        private const string MSG = "An exception occurred while sending a {0} request to Sonarr.";
        internal const string HOW_CAUTION = "can cause editing Sonarr objects (POST/PUT requests) to not perform correctly.  Use caution!";
        internal const string CON_MSG = "Connect-SonarrInstance received a redirect request.  You can have PoshSonarr follow redirects with the '-AllowRedirects' parameter, however this " + HOW_CAUTION;

        public ApiCaller Caller { get; }
        public string RequestMethod { get; }
        public string Url { get; protected set; }

        public BaseSonarrHttpException(string method)
            : base(string.Format(MSG, method))
        {
            this.RequestMethod = method;
        }

        public BaseSonarrHttpException(string message, string method, Exception e)
            : base(message, e)
        {
            this.RequestMethod = method;
        }

        public BaseSonarrHttpException(string method, Exception e)
            : base(string.Format(MSG, method), e)
        {
            this.RequestMethod = method;
        }
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

    public class SonarrConnectException : BaseSonarrHttpException
    {
        public SonarrConnectException(string endpoint, Exception e)
            : base(CON_MSG, HttpMethod.Get.Method, e)
        {
            this.Url = endpoint;
        }
    }

    public class SonarrGetRequestException : BaseSonarrHttpException
    {

        public SonarrGetRequestException(string uriBase)
            : base(HttpMethod.Get.Method) => this.Url = uriBase;
        public SonarrGetRequestException(HttpRequestMessage msg)
            : base(HttpMethod.Get.Method) => this.Url = msg.RequestUri.ToString();

        public SonarrGetRequestException(string uriBase, HttpRequestException hre)
            : base(HttpMethod.Get.Method, hre) => this.Url = uriBase;
    }

    public class SonarrDeleteRequestException : BaseSonarrHttpException
    {
        public SonarrDeleteRequestException(string uriBase, Exception e)
            : base(HttpMethod.Delete.Method, e) => this.Url = uriBase;
    }

    public class SonarrPostRequestException : BaseSonarrHttpException
    {
        public SonarrPostRequestException(string uriBase, Exception e)
            : base(HttpMethod.Post.Method, e)
        {
            this.Url = uriBase;
        }
    }

    public class SonarrPutRequestException : BaseSonarrHttpException
    {
        public SonarrPutRequestException(string uriBase, Exception e)
            : base(HttpMethod.Put.Method, e)
        {
            this.Url = uriBase;
        }
    }
}
