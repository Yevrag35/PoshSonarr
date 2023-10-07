using MG.Sonarr.Next.Exceptions;
using System.Runtime.Serialization;

namespace MG.Sonarr.Next.Shell.Exceptions
{
    /// <summary>
    /// An exception thrown when a blank, <see langword="null"/>, or invalid API key is supplied during 
    /// the initial connection to the Sonarr server.
    /// </summary>
    [Serializable]
    public sealed class InvalidApiKeyException : PoshSonarrException
    {
        const string DEF_MSG = "The supplied Sonarr API key is null, blank, or invalid.";

        /// <summary>
        /// Initializes a new instance of <see cref="InvalidApiKeyException"/> with the default message.
        /// </summary>
        public InvalidApiKeyException()
            : base(DEF_MSG)
        {
        }
        private InvalidApiKeyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
        //public InvalidApiKeyException(Exception? innerException)
        //    : this(DEF_MSG, innerException)
        //{
        //}
        //public InvalidApiKeyException(string message, Exception? innerException)
        //    : base(message, innerException)
        //{
        //}
    }
}
