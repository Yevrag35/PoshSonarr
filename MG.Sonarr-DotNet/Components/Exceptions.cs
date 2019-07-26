using System;

namespace MG.Sonarr
{
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
}
