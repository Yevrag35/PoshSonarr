using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Shell.Context;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets;

public partial class SonarrCmdletBase
{
	// PRE-PROCESSING
	/// <summary>
	/// Stores the cmdlet's Debug preference in <see cref="DebugPreference"/>, from its <c>-Debug</c> switch or, when
	/// that isn't bound, from <c>$DebugPreference</c>, when the cmdlet wants it captured.
	/// </summary>
	/// <param name="wantsCapture">
	/// <see langword="true"/> to store the preference; otherwise, <see langword="false"/> to leave
	/// <see cref="DebugPreference"/> as it is. <see cref="BeginProcessing"/> passes
	/// <see cref="CaptureDebugPreference"/>.
	/// </param>
	[DebuggerStepThrough]
	private void StoreDebugPreference(bool wantsCapture)
	{
		if (!wantsCapture)
		{
			return;
		}

		this.DebugPreference = this.GetActionPreferenceFromSwitch(
			parameterName: PSConstants.Debug,
			variableName: PSConstants.DebugPreference);
	}
	/// <summary>
	/// Stores the cmdlet's Verbose preference in <see cref="VerbosePreference"/>, from its <c>-Verbose</c> switch or,
	/// when that isn't bound, from <c>$VerbosePreference</c>, when the cmdlet wants it captured.
	/// </summary>
	/// <param name="wantsCapture">
	/// <see langword="true"/> to store the preference; otherwise, <see langword="false"/> to leave
	/// <see cref="VerbosePreference"/> as it is. <see cref="BeginProcessing"/> passes
	/// <see cref="CaptureVerbosePreference"/>.
	/// </param>
	[DebuggerStepThrough]
	private void StoreVerbosePreference(bool wantsCapture)
	{
		if (!wantsCapture)
		{
			return;
		}

		this.VerbosePreference = this.GetActionPreferenceFromSwitch(
			parameterName: PSConstants.Verbose,
			variableName: PSConstants.VerbosePreference);
	}

	/// <inheritdoc cref="Cmdlet.BeginProcessing" path="/*[not(self::summary) and not(self::remarks)]"/>
	/// <summary>
	/// Creates the cmdlet's scope, captures its preferences, and runs <see cref="Begin(IServiceProvider)"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method cannot be overridden. Derived cmdlets should instead override the
	/// <see cref="OnCreatingScope(IServiceProvider)"/> and <see cref="Begin(IServiceProvider)"/> methods.
	/// </para>
	/// <para>
	/// An exception from <see cref="OnCreatingScope(IServiceProvider)"/> disposes the scope and ends the statement with
	/// its error, through <see cref="Cmdlet.ThrowTerminatingError(ErrorRecord)"/>. One from
	/// <see cref="Begin(IServiceProvider)"/> ends the cmdlet with its error, as <see cref="StopCmdlet(ErrorRecord)"/>
	/// does: the error goes to the error stream, where <c>-ErrorAction</c> decides what happens to it, and neither
	/// <see cref="Process(IServiceProvider)"/> nor <see cref="End(IServiceProvider)"/> runs.
	/// </para>
	/// <para>
	/// When <see cref="OnCreatingScope(IServiceProvider)"/> stops the cmdlet, through <see cref="StopCmdlet()"/> or
	/// <see cref="StopCmdlet(ErrorRecord)"/>, the method returns without capturing the preferences or running
	/// <see cref="Begin(IServiceProvider)"/>.
	/// </para>
	/// </remarks>
	/// <exception cref="MG.Sonarr.Next.Exceptions.Shell.ContextNotSetException">
	/// Thrown when no connection exists, so the cmdlet can't create its scope.
	/// </exception>
	/// <exception cref="PipelineStoppedException">
	/// Thrown when <see cref="OnCreatingScope(IServiceProvider)"/> throws and the statement ends with its error, when the
	/// error from <see cref="Begin(IServiceProvider)"/> is written under <c>-ErrorAction Stop</c>, and when the pipeline
	/// is stopped.
	/// </exception>
	[DebuggerStepThrough]
	protected sealed override void BeginProcessing()
	{
		_scope = this.CreateScope();
		_isInitialized = true;
		try
		{
			this.OnCreatingScopeInternal(_scope.ServiceProvider);
			this.OnCreatingScope(_scope.ServiceProvider);
		}
		catch (Exception e) when (!IsPipelineControl(e))
		{
			this.Cleanup();
			this.ThrowTerminatingError(e.ToRecord());
		}

		if (_isStopped)
		{
			return;
		}

		try
		{
			this.StoreVerbosePreference(this.CaptureVerbosePreference);
			this.StoreDebugPreference(this.CaptureDebugPreference);
			this.ErrorPreference = this.GetActionPreferenceFromParam(
				parameterName: PSConstants.ErrorAction,
				variableName: PSConstants.ErrorActionPreference,
				defaultIfNotPresent: ActionPreference.Continue);

			this.Begin(_scope.ServiceProvider);
		}
		catch (Exception e) when (!IsPipelineControl(e))
		{
			this.StopCmdlet(e.ToRecord());
		}
	}

	/// <summary>
	/// When overridden in a derived class, initializes the command once, before any input object is processed, with
	/// the services of the cmdlet's scope.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="BeginProcessing"/> calls it after <see cref="OnCreatingScope(IServiceProvider)"/> and after the
	/// cmdlet's preferences are captured. An exception from it ends the cmdlet with its error, as
	/// <see cref="StopCmdlet(ErrorRecord)"/> does, so neither <see cref="Process(IServiceProvider)"/> nor
	/// <see cref="End(IServiceProvider)"/> runs.
	/// </para>
	/// <para>Default implementation in the base class just returns.</para>
	/// </remarks>
	/// <inheritdoc cref="SonarrCmdletBase" path="/provider"/>
	[DebuggerStepThrough]
	protected virtual void Begin(IServiceProvider provider)
	{
		return;
	}

	/// <inheritdoc cref="Cmdlet.ProcessRecord" path="/*[not(self::summary) and not(self::remarks)]"/>
	/// <summary>
	/// Runs <see cref="Process(IServiceProvider)"/> for each input object in the pipeline, unless the cmdlet has
	/// stopped.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method cannot be overridden. Derived cmdlets should instead override the
	/// <see cref="Process(IServiceProvider)"/> method.
	/// </para>
	/// <para>
	/// An exception from <see cref="Process(IServiceProvider)"/> writes its error at once, as a binary cmdlet's
	/// <see cref="Cmdlet.WriteError(ErrorRecord)"/> does, and the cmdlet goes on to the next input object with the same
	/// scope. Under <c>-ErrorAction Stop</c>, writing the error ends the statement there.
	/// </para>
	/// </remarks>
	/// <exception cref="PipelineStoppedException">
	/// Thrown when the error from <see cref="Process(IServiceProvider)"/> is written under <c>-ErrorAction Stop</c>, and
	/// when the pipeline is stopped.
	/// </exception>
	[DebuggerStepThrough]
	protected sealed override void ProcessRecord()
	{
		if (_isStopped || _scope is null)
		{
			return;
		}

		try
		{
			this.Process(_scope.ServiceProvider);
		}
		catch (Exception e) when (!IsPipelineControl(e))
		{
			this.WriteError(e.ToRecord());
		}
	}

	/// <summary>
	/// When overridden in a derived class, processes one input object with the services of the cmdlet's scope.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="ProcessRecord"/> calls it once for each input object, or once when the cmdlet takes no pipeline
	/// input, until the cmdlet stops. An exception from it writes its error, and the cmdlet goes on to the next input
	/// object.
	/// </para>
	/// <para>Default implementation in the base class just returns.</para>
	/// </remarks>
	/// <inheritdoc cref="SonarrCmdletBase" path="/provider"/>
	[DebuggerStepThrough]
	protected virtual void Process(IServiceProvider provider)
	{
		return;
	}

	/// <inheritdoc cref="Cmdlet.EndProcessing" path="/*[not(self::summary) and not(self::remarks)]"/>
	/// <summary>
	/// Runs <see cref="End(IServiceProvider)"/>, unless the cmdlet has stopped, and then disposes the cmdlet's scope.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method cannot be overridden. Derived cmdlets should instead override the
	/// <see cref="End(IServiceProvider)"/> method.
	/// </para>
	/// <para>
	/// An exception from <see cref="End(IServiceProvider)"/> writes its error. The scope is disposed whether or not
	/// <see cref="End(IServiceProvider)"/> ran or threw.
	/// </para>
	/// </remarks>
	/// <exception cref="PipelineStoppedException">
	/// Thrown when the error from <see cref="End(IServiceProvider)"/> is written under <c>-ErrorAction Stop</c>, and
	/// when the pipeline is stopped.
	/// </exception>
	[DebuggerStepThrough]
	protected sealed override void EndProcessing()
	{
		try
		{
			if (!_isStopped && _scope is not null)
			{
				this.End(_scope.ServiceProvider);
			}
		}
		catch (Exception e) when (!IsPipelineControl(e))
		{
			this.WriteError(e.ToRecord());
		}
		finally
		{
			this.Cleanup();
		}
	}

	/// <summary>
	/// When overridden in a derived class, finishes the command once every input object has been processed, with the
	/// services of the cmdlet's scope.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="EndProcessing"/> calls it unless the cmdlet has stopped, and disposes the scope afterward. It doesn't
	/// run when a downstream <c>Select-Object -First</c> or a stop ends the pipeline early, so code that must always
	/// run belongs in an override of <see cref="Dispose(bool)"/>. An exception from it writes its error.
	/// </para>
	/// <para>Default implementation in the base class just returns.</para>
	/// </remarks>
	/// <inheritdoc cref="SonarrCmdletBase" path="/provider"/>
	[DebuggerStepThrough]
	protected virtual void End(IServiceProvider provider)
	{
		return;
	}

	/// <summary>
	/// Cancels <see cref="StoppingToken"/> when the pipeline is stopped, and then runs
	/// <see cref="Stop(IServiceProvider)"/> if the cmdlet has a scope.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method cannot be overridden. Derived cmdlets should instead override the
	/// <see cref="Stop(IServiceProvider)"/> method.
	/// </para>
	/// <para>
	/// PowerShell calls it on another thread while <see cref="BeginProcessing"/>, <see cref="ProcessRecord"/>, or
	/// <see cref="EndProcessing"/> can still be using the scope's services, so it only signals: canceling the token
	/// ends a request in flight, and it neither creates nor disposes a scope, since <see cref="Dispose()"/> disposes
	/// the scope on the pipeline thread once the pipeline ends. Whatever it throws is lost, as is any error it would
	/// report, so it reports none.
	/// </para>
	/// </remarks>
	[DebuggerStepThrough]
	protected sealed override void StopProcessing()
	{
		try
		{
			_stopping.Cancel();
		}
		finally
		{
			if (_scope is { } scope && !_disposed)
			{
				this.Stop(scope.ServiceProvider);
			}
		}
	}
	/// <summary>
	/// When overridden in a derived class, signals the code that runs in the command to stop, such as by canceling the
	/// token of a request in flight.
	/// </summary>
	/// <remarks>
	/// <para>
	/// It runs on another thread while <see cref="Begin(IServiceProvider)"/>, <see cref="Process(IServiceProvider)"/>,
	/// or <see cref="End(IServiceProvider)"/> can still be running, and while the pipeline thread can be disposing the
	/// scope, so it only signals. Whatever it throws is lost.
	/// </para>
	/// <para>Default implementation in the base class just returns.</para>
	/// </remarks>
	/// <param name="provider">The scoped service provider for use by derived cmdlets.</param>
	[DebuggerStepThrough]
	protected virtual void Stop(IServiceProvider provider)
	{
		return;
	}

	/// <summary>
	/// Stops the cmdlet without an error, so that none of its later processing steps run.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The method sets <see cref="IsStopped"/>, which is checked before <see cref="Begin(IServiceProvider)"/>,
	/// <see cref="Process(IServiceProvider)"/>, and <see cref="End(IServiceProvider)"/> run, to determine whether the
	/// next processing step should be skipped, and then calls
	/// <see cref="OnErrorStopping(IServiceProvider, ErrorRecord, bool)"/> with no error.
	/// </para>
	/// <para>
	/// It doesn't end the pipeline: the commands around the cmdlet go on, and the cmdlet takes the rest of its input
	/// without processing it. The scope is still disposed when the pipeline ends. Call it from a processing step, while
	/// the cmdlet holds its scope.
	/// </para>
	/// </remarks>
	[DebuggerStepThrough]
	protected void StopCmdlet()
	{
		_isStopped = true;
		this.OnErrorStopping(_scope!.ServiceProvider, null, errorWasProvided: false);
	}
	/// <summary>
	/// Stops the cmdlet, so that none of its later processing steps run, and writes the specified error to the error
	/// stream.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The method sets <see cref="IsStopped"/>, which is checked before <see cref="Begin(IServiceProvider)"/>,
	/// <see cref="Process(IServiceProvider)"/>, and <see cref="End(IServiceProvider)"/> run, to determine whether the
	/// next processing step should be skipped, calls <see cref="OnErrorStopping(IServiceProvider, ErrorRecord, bool)"/>
	/// with <paramref name="record"/>, and then writes the record through
	/// <see cref="Cmdlet.WriteError(ErrorRecord)"/>, so <c>-ErrorAction</c> decides what happens to it, and under
	/// <c>-ErrorAction Stop</c> the statement ends there.
	/// </para>
	/// <para>
	/// Call it from a processing step, while the cmdlet holds its scope.
	/// </para>
	/// </remarks>
	/// <param name="record">
	/// The error to write, or <see langword="null"/> to stop without one, as <see cref="StopCmdlet()"/> does.
	/// </param>
	/// <exception cref="PipelineStoppedException">
	/// Thrown when the error is written under <c>-ErrorAction Stop</c>.
	/// </exception>
	[DebuggerStepThrough]
	protected void StopCmdlet(ErrorRecord? record)
	{
		if (record is null)
		{
			this.StopCmdlet();
			return;
		}

		_isStopped = true;
		this.OnErrorStopping(_scope!.ServiceProvider, record, errorWasProvided: true);
		this.WriteError(record);
	}

	/// <summary>
	/// Determines whether the cmdlet goes ahead with an action, where <c>-Force</c> skips the confirmation prompt
	/// and nothing else.
	/// </summary>
	/// <remarks>
	/// <para>
	/// In What if mode, which <c>-WhatIf</c> turns on, or <c>$WhatIfPreference</c> when <c>-WhatIf</c> isn't given,
	/// the method calls <see cref="Cmdlet.ShouldProcess(string, string)"/>, which shows the What if line and returns
	/// <see langword="false"/>, whatever <paramref name="force"/> is.
	/// </para>
	/// <para>
	/// Otherwise, when <paramref name="force"/> is <see langword="true"/> and the caller didn't pass <c>-Confirm</c>,
	/// the method returns <see langword="true"/> without prompting. In every other case, it returns what
	/// <see cref="Cmdlet.ShouldProcess(string, string)"/> returns, which prompts as the cmdlet's confirm impact,
	/// <c>-Confirm</c>, and <c>$ConfirmPreference</c> say.
	/// </para>
	/// </remarks>
	/// <param name="target">The resource that the action changes, which the What if line and the prompt name.</param>
	/// <param name="action">The action, which the What if line and the prompt name.</param>
	/// <param name="force">The value of the cmdlet's <c>-Force</c> parameter.</param>
	/// <returns>
	/// <see langword="true"/> if the cmdlet should go ahead with the action; otherwise, <see langword="false"/>.
	/// </returns>
	protected bool ShouldProcessWithForce(string target, string action, bool force)
	{
		return (force && !this.IsWhatIfOn() && !this.IsSwitchBoundOn(PSConstants.Confirm))
			|| this.ShouldProcess(target, action);
	}
	/// <summary>
	/// Determines whether the cmdlet runs in What if mode, from its <c>-WhatIf</c> parameter or, when that isn't
	/// given, from <c>$WhatIfPreference</c>, as <see cref="Cmdlet.ShouldProcess(string, string)"/> decides it.
	/// </summary>
	/// <returns><see langword="true"/> if the cmdlet runs in What if mode; otherwise, <see langword="false"/>.</returns>
	private bool IsWhatIfOn()
	{
		return this.MyInvocation.BoundParameters.ContainsKey(PSConstants.WhatIf)
			? this.IsSwitchBoundOn(PSConstants.WhatIf)
			: LanguagePrimitives.IsTrue(this.SessionState.PSVariable.GetValue(PSConstants.WhatIfPreference));
	}
	/// <summary>
	/// Determines whether a switch parameter is bound and on, whether the caller passed it or
	/// <c>$PSDefaultParameterValues</c> supplied it.
	/// </summary>
	/// <param name="parameterName">The switch parameter's name.</param>
	/// <returns>
	/// <see langword="true"/> if the switch is bound and on; otherwise, <see langword="false"/>, as for
	/// <c>-Confirm:$false</c>.
	/// </returns>
	private bool IsSwitchBoundOn(string parameterName)
	{
		return this.MyInvocation.BoundParameters.TryGetValue(parameterName, out object? bound)
			&& (bound is SwitchParameter sw ? sw.IsPresent : LanguagePrimitives.IsTrue(bound));
	}

	/// <summary>
	/// When overridden in a derived class, sets up the cmdlet's state right after its scope is created and before
	/// <see cref="Begin(IServiceProvider)"/> runs.
	/// </summary>
	/// <remarks>
	///     <para>
	///         This method should be used in lieu of setting member fields/properties in the cmdlet's
	///         constructor. The cmdlet's preferences, such as <see cref="DebugPreference"/>, aren't captured yet.
	///     </para>
	///     <para>
	///         Any exception thrown, other than one that PowerShell throws to end the pipeline or to leave a script
	///         block, disposes the scope and
	///         ends the statement with its error, so no other processing step runs. A call to
	///         <see cref="StopCmdlet()"/> or <see cref="StopCmdlet(ErrorRecord)"/> skips
	///         <see cref="Begin(IServiceProvider)"/> and the rest of the cmdlet's steps instead.
	///     </para>
	///     <para>Default implementation in the base class just returns.</para>
	/// </remarks>
	/// <param name="provider">The scoped service provider for use by derived cmdlets.</param>
	[DebuggerStepThrough]
	protected virtual void OnCreatingScope(IServiceProvider provider)
	{
		return;
	}

	/// <summary>
	/// When overridden in a derived base class of this assembly, resolves the services that the base class needs from
	/// the cmdlet's scope, before <see cref="OnCreatingScope(IServiceProvider)"/> runs.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="SonarrApiCmdletBase"/> resolves its <c>ISonarrClient</c> here, and
	/// <see cref="MG.Sonarr.Next.Shell.Cmdlets.Bases.SonarrMetadataCmdlet"/> its metadata tag, each after calling the base method, so a cmdlet's own
	/// <see cref="OnCreatingScope(IServiceProvider)"/> can already use them. An exception from it is handled as one from
	/// <see cref="OnCreatingScope(IServiceProvider)"/> is.
	/// </para>
	/// <para>Default implementation in the base class just returns.</para>
	/// </remarks>
	/// <param name="provider">The scoped service provider of the cmdlet.</param>
	private protected virtual void OnCreatingScopeInternal(IServiceProvider provider)
	{
		return;
	}

	/// <summary>
	/// When overridden in a derived class, runs when <see cref="StopCmdlet()"/> or
	/// <see cref="StopCmdlet(ErrorRecord)"/> stops the cmdlet, before any error is written.
	/// </summary>
	/// <remarks>
	///     <para>
	///         <see cref="IsStopped"/> is already <see langword="true"/> when it runs.
	///     </para>
	///     <para>
	///         Default implementation in the base class just returns.
	///     </para>
	/// </remarks>
	/// <param name="provider">The scoped service provider for use by derived cmdlets.</param>
	/// <param name="error">
	/// The error that <see cref="StopCmdlet(ErrorRecord)"/> is about to write, or <see langword="null"/> when the
	/// cmdlet stops without one.
	/// </param>
	/// <param name="errorWasProvided">
	/// <see langword="true"/> if <paramref name="error"/> holds an error, and so isn't <see langword="null"/>;
	/// otherwise, <see langword="false"/>.
	/// </param>
	[DebuggerStepThrough]
	protected virtual void OnErrorStopping(IServiceProvider provider, [ParameterNotNullWhenTrue(nameof(errorWasProvided))] ErrorRecord? error, bool errorWasProvided)
	{
		return;
	}
}

