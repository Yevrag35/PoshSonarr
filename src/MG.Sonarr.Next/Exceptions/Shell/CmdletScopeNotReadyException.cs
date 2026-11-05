namespace MG.Sonarr.Next.Exceptions.Shell;

/// <summary>
/// An exception thrown when a PowerShell cmdlet attempts to access the dependency injection scope before it has been initialized.
/// </summary>
public sealed class CmdletScopeNotReadyException : PoshSonarrException
{
	const string DEF_MSG = "The dependency injection scope for the cmdlet has not yet been initialized.";

	/// <summary>
	/// Gets the type of the cmdlet that caused this exception to be thrown.
	/// </summary>
	public Type? CmdletType { get; }

	/// <summary>
	/// Initializes a new instance of <see cref="CmdletScopeNotReadyException"/> with the default message.
	/// </summary>
	public CmdletScopeNotReadyException()
		: base(DEF_MSG)
	{
	}
	/// <summary>
	/// Initializes a new instance of <see cref="CmdletScopeNotReadyException"/> with the default message and the specified cmdlet type.
	/// </summary>
	/// <param name="cmdletType">The type of the cmdlet that caused this exception to be thrown.</param>
	public CmdletScopeNotReadyException(Type? cmdletType)
		: this(cmdletType, null)
	{
	}
	/// <summary>
	/// Initializes a new instance of <see cref="CmdletScopeNotReadyException"/> with the default message, the specified cmdlet type, and the specified inner exception.
	/// </summary>
	/// <param name="cmdletType">The type of the cmdlet that caused this exception to be thrown.</param>
	/// <param name="innerException">The exception that is the cause of the current exception.</param>
	public CmdletScopeNotReadyException(Type? cmdletType, Exception? innerException)
		: base(DEF_MSG, innerException)
	{
		this.CmdletType = cmdletType;
	}

	/// <summary>
	/// Throws a new exception of the type returned by the given <paramref name="exceptionFactory"/>, with this exception as its inner exception.
	/// </summary>
	/// <typeparam name="TOutput">The type of the exception to be thrown.</typeparam>
	/// <param name="cmdlet">The cmdlet that caused this exception to be thrown.</param>
	/// <param name="exceptionFactory">A function that creates a new exception of the specified type.</param>
	/// <returns>Does not return a value; this method always throws an exception.</returns>
	[DoesNotReturn, DebuggerStepThrough, StackTraceHidden]
	public static TOutput ThrowAsInnerTo<TOutput>(Cmdlet cmdlet, Func<CmdletScopeNotReadyException, Exception> exceptionFactory)
	{
		return ThrowAsInnerTo<TOutput>(cmdlet.GetType(), null, exceptionFactory);
	}
	/// <summary>
	/// Throws a new exception of the type returned by the given <paramref name="exceptionFactory"/>, with this exception as its inner exception.
	/// </summary>
	/// <typeparam name="TOutput">The type of the exception to be thrown.</typeparam>
	/// <param name="cmdlet">The cmdlet that caused this exception to be thrown.</param>
	/// <param name="innerException">The exception that is the cause of the current exception.</param>
	/// <param name="exceptionFactory">A function that creates a new exception of the specified type.</param>
	/// <returns>Does not return a value; this method always throws an exception.</returns>
	[DoesNotReturn, DebuggerStepThrough, StackTraceHidden]
	public static TOutput ThrowAsInnerTo<TOutput>(Cmdlet cmdlet, Exception? innerException, Func<CmdletScopeNotReadyException, Exception> exceptionFactory)
	{
		return ThrowAsInnerTo<TOutput>(cmdlet.GetType(), innerException, exceptionFactory);
	}
	/// <summary>
	/// Throws a new exception of the type returned by the given <paramref name="exceptionFactory"/>, with this exception as its inner exception.
	/// </summary>
	/// <typeparam name="TOutput">The type of the exception to be thrown.</typeparam>
	/// <param name="cmdletType">The type of the cmdlet that caused this exception to be thrown.</param>
	/// <param name="innerException">The exception that is the cause of the current exception.</param>
	/// <param name="exceptionFactory">A function that creates a new exception of the specified type.</param>
	/// <returns>Does not return a value; this method always throws an exception.</returns>
	[DoesNotReturn, DebuggerStepThrough, StackTraceHidden]
	public static TOutput ThrowAsInnerTo<TOutput>(Type cmdletType, Exception? innerException, Func<CmdletScopeNotReadyException, Exception> exceptionFactory)
	{
		CmdletScopeNotReadyException ex = new(cmdletType, innerException);
		throw exceptionFactory(ex);
	}
}

