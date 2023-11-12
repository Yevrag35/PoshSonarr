using MG.Sonarr.Next.Exceptions;

namespace MG.Sonarr.Next.Shell.Exceptions
{
    public sealed class ContextNotSetException : PoshSonarrException
    {
        const string DEF_MSG = "The Sonarr context is not set. First run 'Connect-Sonarr' to connect to an instance.";

        public ContextNotSetException(Exception? innerException)
            : base(DEF_MSG, innerException)
        {
        }
    }
}

