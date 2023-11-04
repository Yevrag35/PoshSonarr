using MG.Sonarr.Next.Exceptions;

namespace MG.Sonarr.Next.Shell.Exceptions
{
    public sealed class CmdletScopeNotReadyException : PoshSonarrException
    {
        const string DEF_MSG = "The dependency injection scope for the cmdlet has not yet been initialized.";

        public Type? CmdletType { get; }

        public CmdletScopeNotReadyException()
            : base(DEF_MSG)
        {
        }

        public CmdletScopeNotReadyException(Type? cmdletType)
            : this(cmdletType, null)
        {
        }

        public CmdletScopeNotReadyException(Type? cmdletType, Exception? innerException)
            : base(DEF_MSG, innerException)
        {
            this.CmdletType = cmdletType;
        }
    }
}

