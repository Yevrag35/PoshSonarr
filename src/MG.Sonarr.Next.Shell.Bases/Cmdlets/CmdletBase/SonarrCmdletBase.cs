using MG.Sonarr.Next.Shell.Context;

namespace MG.Sonarr.Next.Shell.Cmdlets;

/// <summary>
/// The base class for all PoshSonarr cmdlets that query or manipulate data from a Sonarr PVR instance.
///
/// <para>
/// When the cmdlet starts the <see cref="BeginProcessing"/> method, an <see cref="IServiceScope"/> is
/// created and used for the entire duration of the cmdlet's execution. The scope is disposed of at the end of
/// <see cref="EndProcessing"/>, or by <see cref="Dispose()"/> when the pipeline ends any other way.
/// </para>
/// </summary>
/// <remarks>
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
	// Never disposed: StopProcessing cancels it on another thread, which Dispose would race, and a source whose
	// WaitHandle nothing reads holds nothing that needs disposing.
	private readonly CancellationTokenSource _stopping = new();

	[MemberNotNullWhen(true, nameof(Services), nameof(_scope))]
	protected private bool IsScopeInitialized => _isInitialized;
	protected private IServiceProvider? Services => _scope?.ServiceProvider;

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
	/// Gets a token that is canceled when the pipeline is stopped, as Ctrl+C stops it.
	/// </summary>
	/// <remarks>
	/// <see cref="StopProcessing"/> cancels it on another thread, so a request or a wait that takes it ends as soon as
	/// the pipeline is stopped rather than when it would have finished. The <c>Send*Request</c> helpers of
	/// <see cref="SonarrApiCmdletBase"/> send with it unless they're given a token of their own.
	/// </remarks>
	protected CancellationToken StoppingToken => _stopping.Token;

	/// <summary>
	/// Specifies whether derived cmdlets should capture the current Debug <see cref="ActionPreference"/> value
	/// before cmdlet processing begins.
	/// </summary>
	/// <remarks>
	///     Default implementation in the base class always returns <see langword="true"/>.
	/// </remarks>
	protected virtual bool CaptureDebugPreference { get; } = true;

	/// <summary>
	/// Specifies whether derived cmdlets should capture the current Verbose <see cref="ActionPreference"/> value
	/// before cmdlet processing begins.
	/// </summary>
	/// <remarks>
	///     Default implementation in the base class always returns <see langword="false"/>.
	/// </remarks>
	protected virtual bool CaptureVerbosePreference { get; }

	/// <summary>
	/// The current Debug preference either set from the "-Debug" <see cref="SwitchParameter"/>
	/// or read from the global "$DebugPreference" variable.
	/// </summary>
	/// <remarks>
	///     This value is set at the very beginning of the <see cref="BeginProcessing"/> execution.
	/// </remarks>
	protected ActionPreference DebugPreference { get; private set; }
	/// <summary>
	/// The current error preference either set from the "-ErrorAction" parameter
	/// or read from the global "$ErrorActionPreference" variable.
	/// </summary>
	/// <remarks>
	///     This value is set at the very beginning of the <see cref="BeginProcessing"/> execution.
	/// </remarks>
	protected ActionPreference ErrorPreference { get; private set; }

	/// <summary>
	/// The current verbosity preference either set from the "-Verbose" <see cref="SwitchParameter"/>
	/// or read from the global "$VerbosePreference" variable.
	/// </summary>
	/// <remarks>
	///     This value is set at the very beginning of the <see cref="BeginProcessing"/> execution.
	/// </remarks>
	protected ActionPreference VerbosePreference { get; private set; }

	#region EXCEPTIONS

	const string PIPELINE_STOPPED = "The pipeline has been stopped.";

	/// <typeparam name="T">The output type of this method. It will never be returned.</typeparam>
	/// <exception cref="PipelineStoppedException"></exception>
	[DoesNotReturn]
	protected static T ThrowPipelineStopped<T>()
	{
		throw new PipelineStoppedException(PIPELINE_STOPPED);
	}

	/// <inheritdoc cref="ThrowPipelineStopped{T}()"/>
	/// <param name="innerException"><inheritdoc cref="Exception(string?, Exception?)"
	///     path="/param[last()]"/></param>
	[DoesNotReturn]
	protected static T ThrowPipelineStopped<T>(Exception? innerException)
	{
		if (innerException is null)
		{
			return ThrowPipelineStopped<T>();
		}
		else
		{
			return ThrowPipelineStopped<T>(innerException.Message, innerException);
		}
	}

	/// <inheritdoc cref="Exception(string, Exception)" path="/*[not(self::summary)]"/>
	/// <exception cref="PipelineStoppedException"/>
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

	#endregion

	private static bool _hasChecked;
	static bool IScopeCmdlet<SonarrCmdletBase>.HasChecked()
	{
		return _hasChecked;
	}
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

	#region CLEANUP

	protected virtual void Cleanup()
	{
		this.Dispose();
	}

	#region DISPOSAL
	/// <summary>
	/// Disposes the cmdlet's scoped <see cref="IServiceProvider"/> once the <see cref="SonarrCmdletBase"/> has
	/// finished execution.
	/// </summary>
	/// <remarks>
	/// A derived cmdlet that overrides it releases its own resources first and then calls the base method.
	/// </remarks>
	/// <param name="disposing">The method should dispose of the scope.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			_scope?.Dispose();
			_scope = null!;
			_disposed = true;
		}
	}

	[DebuggerStepThrough]
	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		this.Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion

	#endregion
}
