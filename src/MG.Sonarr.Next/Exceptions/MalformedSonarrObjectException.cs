namespace MG.Sonarr.Next.Exceptions
{
    public sealed class MalformedSonarrObjectException : PoshSonarrException
    {
        public object? MalformedObject { get; }

        public MalformedSonarrObjectException(string? message, object? malformedObject)
            : this(message, malformedObject, innerException: null)
        {
        }
        public MalformedSonarrObjectException(string? message, object? malformedObject, Exception? innerException)
            : base(message, innerException)
        {
            this.MalformedObject = malformedObject;
        }
    }
}
