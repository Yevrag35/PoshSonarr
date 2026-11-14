using MG.Sonarr.Next.Shell.Context;

namespace MG.Sonarr.Next.Shell.Cmdlets;

/// <summary>
/// Represents the base class for all PoshSonarr cmdlets that query or manipulate data from a Sonarr PVR instance.
/// </summary>
/// <remarks>
/// <para>
/// When the cmdlet starts the <see cref="BeginProcessing"/> method, an <see cref="IServiceScope"/> is
/// created and used for the entire duration of the cmdlet's execution. The scope is disposed of at the end of
/// <see cref="EndProcessing"/>, or by <see cref="Dispose()"/> when the pipeline ends any other way.
/// </para>
/// <para>
/// PowerShell calls <see cref="Dispose()"/> once the pipeline ends, however it ends: normally, through a downstream
/// <c>Select-Object -First</c>, through a stop, or through an error that ends the statement. So the scope is disposed
/// on the pipeline thread even when <see cref="EndProcessing"/> doesn't run, and a derived cmdlet releases its own
/// resources in an override of <see cref="Dispose(bool)"/>.
/// </para>
/// <para>
/// An exception from <see cref="Begin(IServiceProvider)"/> ends the cmdlet with its error, which is written to the
/// error stream, and neither <see cref="Process(IServiceProvider)"/> nor <see cref="End(IServiceProvider)"/> runs. An
/// exception from <see cref="Process(IServiceProvider)"/> writes its error at once, and the cmdlet goes on to the next
/// input. Either way, <c>-ErrorAction</c> decides what happens to the error, so under <c>-ErrorAction Stop</c> the
/// statement ends at the step that threw.
/// </para>
/// </remarks>
//[DebuggerStepThrough]
public abstract partial class SonarrCmdletBase : PSCmdlet,
	IDisposable,
	IIsRunning<SonarrCmdletBase>,
	IScopeCmdlet<SonarrCmdletBase>
{
	private bool _disposed;
	private bool _isInitialized;
	private bool _isStopped;
	private IServiceScope? _scope;

	/// <summary>
	/// Gets a value that indicates whether <see cref="BeginProcessing"/> has created the cmdlet's scope.
	/// </summary>
	/// <remarks>
	/// The flag is set as soon as <see cref="BeginProcessing"/> creates the scope, before
	/// <see cref="OnCreatingScopeInternal(IServiceProvider)"/> and <see cref="OnCreatingScope(IServiceProvider)"/> run,
	/// and the annotation promises that <see cref="Services"/> and <c>_scope</c> are set while it's
	/// <see langword="true"/>, so it's meant to read <see langword="true"/> only until <see cref="Dispose(bool)"/>
	/// disposes the scope.
	/// </remarks>
	/// <value>
	/// <see langword="true"/> while the cmdlet holds its scope; otherwise, <see langword="false"/>.
	/// </value>
	[MemberNotNullWhen(true, nameof(Services), nameof(_scope))]
	private protected bool IsScopeInitialized => _isInitialized;
	/// <summary>
	/// Gets the service provider of the cmdlet's scope.
	/// </summary>
	/// <value>
	/// The scope's <see cref="IServiceProvider"/>, or <see langword="null"/> before <see cref="BeginProcessing"/> has
	/// created the scope and after <see cref="Dispose(bool)"/> has disposed it.
	/// </value>
	private protected IServiceProvider? Services => _scope?.ServiceProvider;

	/// <summary>
	/// Gets a value that indicates whether the cmdlet has stopped, through <see cref="StopCmdlet()"/>, through
	/// <see cref="StopCmdlet(ErrorRecord)"/>, or because <see cref="Begin(IServiceProvider)"/> threw.
	/// </summary>
	/// <value>
	/// <see langword="true"/> once the cmdlet has stopped, after which no <see cref="Process(IServiceProvider)"/> or
	/// <see cref="End(IServiceProvider)"/> runs; otherwise, <see langword="false"/>.
	/// </value>
	protected bool IsStopped => _isStopped;

	/// <summary>
	/// When overridden in a derived class, gets a value that indicates whether the cmdlet captures its Debug
	/// <see cref="ActionPreference"/> into <see cref="DebugPreference"/> before <see cref="Begin(IServiceProvider)"/>
	/// runs.
	/// </summary>
	/// <remarks>
	///     Default implementation in the base class always returns <see langword="true"/>.
	/// </remarks>
	/// <value>
	/// <see langword="true"/> if the cmdlet captures its Debug preference; otherwise, <see langword="false"/>, which
	/// leaves <see cref="DebugPreference"/> at <see cref="ActionPreference.SilentlyContinue"/>.
	/// </value>
	protected virtual bool CaptureDebugPreference { get; } = true;

	/// <summary>
	/// When overridden in a derived class, gets a value that indicates whether the cmdlet captures its Verbose
	/// <see cref="ActionPreference"/> into <see cref="VerbosePreference"/> before <see cref="Begin(IServiceProvider)"/>
	/// runs.
	/// </summary>
	/// <remarks>
	///     Default implementation in the base class always returns <see langword="false"/>.
	/// </remarks>
	/// <value>
	/// <see langword="true"/> if the cmdlet captures its Verbose preference; otherwise, <see langword="false"/>, which
	/// leaves <see cref="VerbosePreference"/> at <see cref="ActionPreference.SilentlyContinue"/>.
	/// </value>
	protected virtual bool CaptureVerbosePreference { get; }

	/// <summary>
	/// Gets the cmdlet's Debug preference, from its <c>-Debug</c> switch or, when that isn't bound, from
	/// <c>$DebugPreference</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="BeginProcessing"/> sets it after <see cref="OnCreatingScope(IServiceProvider)"/> runs and before
	/// <see cref="Begin(IServiceProvider)"/> runs, and only when <see cref="CaptureDebugPreference"/> is
	/// <see langword="true"/>.
	/// </para>
	/// <para>
	/// A bound <c>-Debug</c> gives <see cref="ActionPreference.Continue"/>, and <c>-Debug:$false</c> gives
	/// <see cref="ActionPreference.SilentlyContinue"/>. Otherwise, the value of <c>$DebugPreference</c> in the scope that
	/// the cmdlet runs in is converted to an <see cref="ActionPreference"/>, and a variable that doesn't exist or doesn't
	/// convert gives <see cref="ActionPreference.SilentlyContinue"/>.
	/// </para>
	/// </remarks>
	/// <value>The Debug preference that the cmdlet runs with.</value>
	protected ActionPreference DebugPreference { get; private set; }
	/// <summary>
	/// Gets the cmdlet's error preference, from its <c>-ErrorAction</c> parameter or, when that isn't bound, from
	/// <c>$ErrorActionPreference</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="BeginProcessing"/> sets it after <see cref="OnCreatingScope(IServiceProvider)"/> runs and before
	/// <see cref="Begin(IServiceProvider)"/> runs.
	/// </para>
	/// <para>
	/// A bound <c>-ErrorAction</c> gives its value. Otherwise, the value of <c>$ErrorActionPreference</c> in the scope
	/// that the cmdlet runs in is converted to an <see cref="ActionPreference"/>. A bound value that names no
	/// <see cref="ActionPreference"/> member, and a variable that doesn't exist or doesn't convert, give
	/// <see cref="ActionPreference.Continue"/>.
	/// </para>
	/// </remarks>
	/// <value>The error preference that the cmdlet runs with.</value>
	protected ActionPreference ErrorPreference { get; private set; }

	/// <summary>
	/// Gets the cmdlet's Verbose preference, from its <c>-Verbose</c> switch or, when that isn't bound, from
	/// <c>$VerbosePreference</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="BeginProcessing"/> sets it after <see cref="OnCreatingScope(IServiceProvider)"/> runs and before
	/// <see cref="Begin(IServiceProvider)"/> runs, and only when <see cref="CaptureVerbosePreference"/> is
	/// <see langword="true"/>.
	/// </para>
	/// <para>
	/// A bound <c>-Verbose</c> gives <see cref="ActionPreference.Continue"/>, and <c>-Verbose:$false</c> gives
	/// <see cref="ActionPreference.SilentlyContinue"/>. Otherwise, the value of <c>$VerbosePreference</c> in the scope
	/// that the cmdlet runs in is converted to an <see cref="ActionPreference"/>, and a variable that doesn't exist or
	/// doesn't convert gives <see cref="ActionPreference.SilentlyContinue"/>.
	/// </para>
	/// </remarks>
	/// <value>The Verbose preference that the cmdlet runs with.</value>
	protected ActionPreference VerbosePreference { get; private set; }

	// EXCEPTIOND
	private const string PIPELINE_STOPPED = "The pipeline has been stopped.";

	/// <summary>
	/// Throws a <see cref="PipelineStoppedException"/> with the message "The pipeline has been stopped."
	/// </summary>
	/// <remarks>
	/// The return type lets a caller throw from an expression, such as a <c>switch</c> arm or a <c>??</c> operand.
	/// </remarks>
	/// <typeparam name="T">The output type of this method. It will never be returned.</typeparam>
	/// <returns>This method never returns.</returns>
	/// <exception cref="PipelineStoppedException">Thrown when the method is called.</exception>
	[DoesNotReturn, DebuggerStepThrough, StackTraceHidden]
	protected static T ThrowPipelineStopped<T>()
	{
		throw new PipelineStoppedException(PIPELINE_STOPPED);
	}

	/// <summary>
	/// Throws a <see cref="PipelineStoppedException"/> that wraps the specified exception.
	/// </summary>
	/// <remarks>
	/// The new exception takes the message of <paramref name="innerException"/>, or "The pipeline has been stopped."
	/// when that message is empty or <paramref name="innerException"/> is <see langword="null"/>.
	/// </remarks>
	/// <typeparam name="T">The output type of this method. It will never be returned.</typeparam>
	/// <param name="innerException">
	/// The exception that caused the stop, or <see langword="null"/> to throw one with no inner exception.
	/// </param>
	/// <returns>This method never returns.</returns>
	/// <exception cref="PipelineStoppedException">Thrown when the method is called.</exception>
	[DoesNotReturn, DebuggerStepThrough, StackTraceHidden]
	protected static T ThrowPipelineStopped<T>(Exception? innerException)
	{
		return innerException is null
			? ThrowPipelineStopped<T>()
			: ThrowPipelineStopped<T>(innerException.Message, innerException);
	}

	/// <summary>
	/// Throws a <see cref="PipelineStoppedException"/> with the specified message and inner exception.
	/// </summary>
	/// <remarks>
	/// When <paramref name="message"/> is <see langword="null"/> or empty, the new exception takes the message of
	/// <paramref name="innerException"/>, or "The pipeline has been stopped." when that message is empty or
	/// <paramref name="innerException"/> is <see langword="null"/>.
	/// </remarks>
	/// <typeparam name="T">The output type of this method. It will never be returned.</typeparam>
	/// <param name="message">The message of the new exception, or <see langword="null"/> for a default one.</param>
	/// <param name="innerException">
	/// The exception that caused the stop, or <see langword="null"/> to throw one with no inner exception.
	/// </param>
	/// <returns>This method never returns.</returns>
	/// <exception cref="PipelineStoppedException">Thrown when the method is called.</exception>
	[DoesNotReturn]
	protected static T ThrowPipelineStopped<T>(string? message, Exception? innerException)
	{
		if (string.IsNullOrEmpty(message))
		{
			if (innerException is null)
			{
				return ThrowPipelineStopped<T>();
			}

			message = string.IsNullOrEmpty(innerException.Message)
				? PIPELINE_STOPPED
				: innerException.Message;
		}

		throw new PipelineStoppedException(message, innerException);
	}

	private static bool _hasChecked;
	/// <summary>
	/// Gets the flag that <see cref="IScopeCmdlet{T}.SetChecked(bool)"/> last set for every
	/// <see cref="SonarrCmdletBase"/> cmdlet in the process.
	/// </summary>
	/// <remarks>
	/// <para>The flag is static, so every runspace shares it.</para>
	/// <para>TODO: Clarify what the flag records. Nothing in the solution calls this method or its setter.</para>
	/// </remarks>
	/// <returns><see langword="true"/> if the flag is set; otherwise, <see langword="false"/>.</returns>
	static bool IScopeCmdlet<SonarrCmdletBase>.HasChecked()
	{
		return _hasChecked;
	}
	/// <summary>
	/// Sets the flag that <see cref="IScopeCmdlet{T}.HasChecked"/> returns for every <see cref="SonarrCmdletBase"/>
	/// cmdlet in the process.
	/// </summary>
	/// <remarks>
	/// <para>The flag is static, so every runspace shares it.</para>
	/// <para>TODO: Clarify what the flag records. Nothing in the solution calls this method or its getter.</para>
	/// </remarks>
	/// <param name="toggle">The flag's new value.</param>
	static void IScopeCmdlet<SonarrCmdletBase>.SetChecked(bool toggle)
	{
		_hasChecked = toggle;
	}

	/// <summary>
	/// Determines whether an exception is one that PowerShell throws to end the pipeline or to leave a script block,
	/// which the base class lets through rather than writing it as the cmdlet's error.
	/// </summary>
	/// <remarks>
	/// A cmdlet sees a <see cref="PipelineStoppedException"/> when the pipeline is stopped, when a downstream
	/// <c>Select-Object -First</c> has what it needs, when <see cref="Cmdlet.ThrowTerminatingError(ErrorRecord)"/> runs,
	/// and when <see cref="Cmdlet.WriteError(ErrorRecord)"/> runs under <c>-ErrorAction Stop</c>, which PowerShell has
	/// already reported, so writing it again would report the error twice. A <see cref="FlowControlException"/> carries
	/// <c>break</c>, <c>continue</c>, <c>return</c>, or <c>exit</c> out of a script block that the cmdlet runs.
	/// </remarks>
	/// <param name="exception">The exception that a processing step threw.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="exception"/> belongs to PowerShell; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool IsPipelineControl(Exception exception)
	{
		return exception is PipelineStoppedException or ActionPreferenceStopException or FlowControlException;
	}

	// CLEANUP
	/// <summary>
	/// When overridden in a derived class, releases what the cmdlet holds once it has finished, or once
	/// <see cref="OnCreatingScope(IServiceProvider)"/> has failed.
	/// </summary>
	/// <remarks>
	/// <see cref="EndProcessing"/> calls it after <see cref="End(IServiceProvider)"/>, and <see cref="BeginProcessing"/>
	/// calls it before it ends the statement with the error from <see cref="OnCreatingScope(IServiceProvider)"/>. The
	/// base implementation calls <see cref="Dispose()"/>, which disposes the scope, so an override calls the base method.
	/// </remarks>
	protected virtual void Cleanup()
	{
		this.Dispose();
	}

	// DISPOSAL
	/// <summary>
	/// Disposes the cmdlet's scoped <see cref="IServiceProvider"/> once the <see cref="SonarrCmdletBase"/> has
	/// finished execution.
	/// </summary>
	/// <remarks>
	/// A derived cmdlet that overrides it releases its own resources first and then calls the base method.
	/// </remarks>
	/// <param name="disposing">
	/// <see langword="true"/> when <see cref="Dispose()"/> calls the method; otherwise, <see langword="false"/>. The base
	/// implementation disposes the scope either way, and only the first call does anything.
	/// </param>
	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			_scope?.Dispose();
			_scope = null!;
			_disposed = true;
		}
	}

	/// <summary>
	/// Disposes the cmdlet's scope, if it still holds one.
	/// </summary>
	/// <remarks>
	/// PowerShell calls it once the pipeline ends, however it ends, so the scope is disposed even when
	/// <see cref="EndProcessing"/> doesn't run. It calls <see cref="Dispose(bool)"/>, and calling it again does nothing.
	/// </remarks>
	[DebuggerStepThrough]
	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		this.Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
