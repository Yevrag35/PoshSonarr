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

        //[DoesNotReturn]
        //[DebuggerStepThrough]
        //public static TOutput ThrowAsInnerTo<TCmdlet, TOutput>(Func<CmdletScopeNotReadyException, Exception> exceptionFactory)
        //    where TCmdlet : Cmdlet
        //{
        //    return ThrowAsInnerTo<TOutput>(typeof(TCmdlet), null, exceptionFactory);
        //}

        //[DoesNotReturn]
        //[DebuggerStepThrough]
        //public static TOutput ThrowAsInnerTo<TCmdlet, TOutput>(Exception? innerException, Func<CmdletScopeNotReadyException, Exception> exceptionFactory)
        //    where TCmdlet : Cmdlet
        //{
        //    return ThrowAsInnerTo<TOutput>(typeof(TCmdlet), innerException, exceptionFactory);
        //}

        [DoesNotReturn]
        [DebuggerStepThrough]
        public static TOutput ThrowAsInnerTo<TOutput>(Cmdlet cmdlet, Func<CmdletScopeNotReadyException, Exception> exceptionFactory)
        {
            return ThrowAsInnerTo<TOutput>(cmdlet.GetType(), null, exceptionFactory);
        }

        [DoesNotReturn]
        [DebuggerStepThrough]
        public static TOutput ThrowAsInnerTo<TOutput>(Cmdlet cmdlet, Exception? innerException, Func<CmdletScopeNotReadyException, Exception> exceptionFactory)
        {
            return ThrowAsInnerTo<TOutput>(cmdlet.GetType(), innerException, exceptionFactory);
        }

        [DoesNotReturn]
        [DebuggerStepThrough]
        public static TOutput ThrowAsInnerTo<TOutput>(Type cmdletType, Exception? innerException, Func<CmdletScopeNotReadyException, Exception> exceptionFactory)
        {
            CmdletScopeNotReadyException ex = new(cmdletType, innerException);
            throw exceptionFactory(ex);
        }
    }
}

