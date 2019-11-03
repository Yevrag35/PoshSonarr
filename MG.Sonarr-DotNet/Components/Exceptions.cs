using MG.Sonarr.Cmdlets;
using System;
using System.Management.Automation;
using System.Net;
using System.Net.Http;

namespace MG.Sonarr
{
    /// <summary>
    /// The <see cref="Exception"/> base class for all Sonarr exceptions.
    /// </summary>
    public abstract class BaseSonarrHttpException : HttpRequestException
    {
        internal const string CAUTION = "Allowing redirects ";
        private const string MSG = "An exception occurred while sending a {0} request to Sonarr.";
        internal const string HOW_CAUTION = "can cause editing Sonarr objects (POST/PUT requests) to not perform correctly.  Use caution!";
        internal const string CON_MSG = "Connect-SonarrInstance received a redirect request.  You can have PoshSonarr follow redirects with the '-AllowRedirects' parameter, however this " + HOW_CAUTION;

        /// <summary>
        /// A copy of the current context's <see cref="HttpClient"/>.
        /// </summary>
        public SonarrRestClient Caller { get; }

        /// <summary>
        /// The request method that caused this exception to be thrown.
        /// </summary>
        public string RequestMethod { get; }

        /// <summary>
        /// The API url from the offending <see cref="HttpRequestException"/>.
        /// </summary>
        public string Url { get; protected set; }

        public BaseSonarrHttpException(string method)
            : base(string.Format(MSG, method))
        {
            this.RequestMethod = method;
        }

        public BaseSonarrHttpException(string message, string method)
            : base(message)
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

    /// <summary>
    /// The exception that is thrown when an invalid <see cref="ApiKey"/> is supplied to <see cref="ConnectInstance"/>.
    /// </summary>
    public class InvalidApiKeyException : ArgumentException
    {
        private const string EX_MSG = "The input key specified is not of the valid format. (32 characters; all lowercase).";
        public InvalidApiKeyException()
            : base(EX_MSG) { }
    }

    /// <summary>
    /// The exception that is thrown when the "-PassThru" parameter is specified on the <see cref="ConnectInstance"/> cmdlet and no response was received from the Sonarr server.
    /// </summary>
    public class NoSonarrResponseException : BaseSonarrHttpException
    {
        private const string MSG = "No response was received from the Sonarr server.";

        public NoSonarrResponseException()
            : base(MSG, HttpMethod.Get.Method)
        {
        }
    }

    /// <summary>
    /// The exception thrown when an HttpRequestException is thrown during the processing of the <see cref="ConnectInstance"/> cmdlet.
    /// </summary>
    public class SonarrConnectException : BaseSonarrHttpException
    {
        public SonarrConnectException(string endpoint, Exception e)
            : base(CON_MSG, HttpMethod.Get.Method, e)
        {
            this.Url = endpoint;
        }
    }

    /// <summary>
    /// The exception thrown when any PoshSonarr cmdlet (other than <see cref="ConnectInstance"/>) is attempted before the <see cref="Context"/> is set.
    /// </summary>
    public class SonarrContextNotSetException : InvalidOperationException
    {
        private const string defMsg = "The Sonarr context is not set!{0}.  Run the \"Connect-SonarrInstance\" cmdlet first.";

        public SonarrContextNotSetException()
            : base(string.Format(defMsg, string.Empty))
        {
        }

        public SonarrContextNotSetException(string additionalMsg)
            : base(string.Format(defMsg, additionalMsg))
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when <see cref="BaseSonarrCmdlet.TryGetSonarrResult(string)"/> encounters an <see cref="HttpRequestException"/>.
    /// </summary>
    public class SonarrGetRequestException : BaseSonarrHttpException
    {
        public SonarrGetRequestException(string uriBase)
            : base(HttpMethod.Get.Method) => this.Url = uriBase;
        public SonarrGetRequestException(HttpRequestMessage msg)
            : base(HttpMethod.Get.Method) => this.Url = msg.RequestUri.ToString();

        public SonarrGetRequestException(string uriBase, HttpRequestException hre)
            : base(HttpMethod.Get.Method, hre) => this.Url = uriBase;
    }

    /// <summary>
    /// The exception that is thrown when <see cref="BaseSonarrCmdlet.TryDeleteSonarrResult(string)"/> encounters an <see cref="HttpRequestException"/>.
    /// </summary>
    public class SonarrDeleteRequestException : BaseSonarrHttpException
    {
        public SonarrDeleteRequestException(string uriBase, Exception e)
            : base(HttpMethod.Delete.Method, e) => this.Url = uriBase;
    }

    /// <summary>
    /// The exception that is thrown when <see cref="BaseSonarrCmdlet.TryPostSonarrResult(string, string)"/> encounters an <see cref="HttpRequestException"/>.
    /// </summary>
    public class SonarrPostRequestException : BaseSonarrHttpException
    {
        public SonarrPostRequestException(string uriBase, Exception e)
            : base(HttpMethod.Post.Method, e)
        {
            this.Url = uriBase;
        }
    }

    /// <summary>
    /// The exception that is thrown when <see cref="BaseSonarrCmdlet.TryPutSonarrResult(string, string)"/> encounters an <see cref="HttpRequestException"/>.
    /// </summary>
    public class SonarrPutRequestException : BaseSonarrHttpException
    {
        public SonarrPutRequestException(string uriBase, Exception e)
            : base(HttpMethod.Put.Method, e)
        {
            this.Url = uriBase;
        }
    }
}
