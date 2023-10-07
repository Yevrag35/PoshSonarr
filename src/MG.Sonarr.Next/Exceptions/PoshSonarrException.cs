using System.Runtime.Serialization;

namespace MG.Sonarr.Next.Exceptions
{
    /// <summary>
    /// An <see langword="abstract"/> base class for all <see cref="Exception"/> instances thrown by PoshSonarr libraries.
    /// </summary>
    [Serializable]
    public class PoshSonarrException : Exception
    {
        protected PoshSonarrException(string message)
            : base(message)
        {
        }
        protected PoshSonarrException(string message, Exception? innerException)
            : base(message, innerException)
        {
        }
        protected PoshSonarrException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
