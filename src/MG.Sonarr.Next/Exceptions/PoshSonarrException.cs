using System.Runtime.Serialization;

namespace MG.Sonarr.Next.Exceptions
{
    /// <summary>
    /// An <see langword="abstract"/> base class for all <see cref="Exception"/> instances thrown by PoshSonarr libraries.
    /// </summary>
    [Serializable]
    public class PoshSonarrException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PoshSonarrException"/> class with a specified
        /// error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        protected PoshSonarrException(string message)
            : base(message)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="PoshSonarrException"/> class with a specified error
        /// message and a reference to an inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">
        ///     The exception that is the casue of the current exception, or a <see langword="null"/> reference
        ///     if no inner exception is specified.
        /// </param>
        protected PoshSonarrException(string message, Exception? innerException)
            : base(message, innerException)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="PoshSonarrException"/> class with serialized data.
        /// </summary>
        /// <param name="info">
        ///     The <see cref="SerializationInfo"/> that holds the serialized object data about the 
        ///     exception being thrown.
        /// </param>
        /// <param name="context">
        ///     The <see cref="StreamingContext"/> that contains contexual information about the source
        ///     or destination.
        /// </param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="SerializationException"/>
        protected PoshSonarrException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
