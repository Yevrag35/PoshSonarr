using MG.Sonarr.Next.Attributes;
using System.Net;
using System.Runtime.Serialization;

namespace MG.Sonarr.Next.Exceptions
{
    /// <summary>
    /// An exception that is throw when an <see cref="HttpRequestMessage"/> did not indicate an error
    /// <see cref="HttpStatusCode"/>, but no response content was returned when one was expected.
    /// </summary>
    [Serializable]
    public sealed class EmptyHttpResponseException : PoshSonarrException
    {
        const string MSG = "An empty response was received when there should have been one.";

        public string Url { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmptyHttpResponseException"/> class with the 
        /// specified URL that the exception occurred from.
        /// </summary>
        /// <param name="url">The <see cref="string"/> url of the request.</param>
        public EmptyHttpResponseException(string url)
            : base(MSG)
        {
            this.Url = url;
        }
        private EmptyHttpResponseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ArgumentNullException.ThrowIfNull(info);
            this.Url = info.GetString(nameof(this.Url)) ?? string.Empty;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Guard.NotNull(info);
            ArgumentNullException.ThrowIfNull(info);
            info.AddValue(nameof(this.Url), this.Url, typeof(string));

            base.GetObjectData(info, context);
        }
    }
}
