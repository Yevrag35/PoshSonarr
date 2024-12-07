using MG.Sonarr.Next.Exceptions;

namespace MG.Sonarr.Next.Shell.Exceptions
{
    /// <summary>
    /// An exception thrown when a blank, <see langword="null"/>, or invalid API key is supplied during 
    /// the initial connection to the Sonarr server.
    /// </summary>
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
    }
}
