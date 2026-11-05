using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Shell.Context;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets;

/// <provider>
///     <param name="provider">The scoped service provider for use by derived cmdlets.</param>
/// </provider>
public abstract partial class SonarrCmdletBase
{
	#region PRE-PROCESSING
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

	#endregion

	/// <inheritdoc cref="Cmdlet.BeginProcessing" path="/*[not(self::summary) and not(self::remarks)]"/>
	/// <summary>
	/// Creates the cmdlet's scope, captures its preferences, and runs <see cref="Begin(IServiceProvider)"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method cannot be overriden. Derived cmdlets should instead override the
	/// <see cref="OnCreatingScope(IServiceProvider)"/> and <see cref="Begin(IServiceProvider)"/> methods.
	/// </para>
	/// <para>
	/// An exception from <see cref="OnCreatingScope(IServiceProvider)"/> disposes the scope and ends the statement with
	/// its error, through <see cref="Cmdlet.ThrowTerminatingError(ErrorRecord)"/>. One from
	/// <see cref="Begin(IServiceProvider)"/> ends the cmdlet with its error, as <see cref="StopCmdlet(ErrorRecord)"/>
	/// does: the error goes to the error stream, where <c>-ErrorAction</c> decides what happens to it, and neither
	/// <see cref="Process(IServiceProvider)"/> nor <see cref="End(IServiceProvider)"/> runs.
	/// </para>
	/// </remarks>
	/// <exception cref="PipelineStoppedException"/>
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

	/// <inheritdoc cref="SonarrCmdletBase" path="/provider"/>
	/// <inheritdoc cref="Cmdlet.BeginProcessing" path="/*[not(self::summary)]"/>
	/// <summary>
	/// When overridden in the derived <see cref="SonarrCmdletBase"/> cmdlet, performs initialization of 
	/// command execution using the provided <see cref="IServiceProvider"/> of the scope cmdlet.
	/// </summary>
	/// <remarks>Default implementation in the base class just returns.</remarks>
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
	/// This method cannot be overriden. Derived cmdlets should instead override the
	/// <see cref="Process(IServiceProvider)"/> method.
	/// </para>
	/// <para>
	/// An exception from <see cref="Process(IServiceProvider)"/> writes its error at once, as a binary cmdlet's
	/// <see cref="Cmdlet.WriteError(ErrorRecord)"/> does, and the cmdlet goes on to the next input object with the same
	/// scope. Under <c>-ErrorAction Stop</c>, writing the error ends the statement there.
	/// </para>
	/// </remarks>
	/// <exception cref="PipelineStoppedException"/>
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

	/// <inheritdoc cref="SonarrCmdletBase" path="/provider"/>
	/// <inheritdoc cref="Cmdlet.ProcessRecord" path="/*[not(self::summary)]"/>
	/// <summary>
	/// When overridden in the derived <see cref="SonarrCmdletBase"/> cmdlet, performs execution of 
	/// the command using the provided <see cref="IServiceProvider"/> of the scope cmdlet.
	/// Default implementation in the base class just returns.
	/// </summary>
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
	/// This method cannot be overriden. Derived cmdlets should instead override the
	/// <see cref="End(IServiceProvider)"/> method.
	/// </para>
	/// <para>
	/// An exception from <see cref="End(IServiceProvider)"/> writes its error. The scope is disposed whether or not
	/// <see cref="End(IServiceProvider)"/> ran or threw.
	/// </para>
	/// </remarks>
	/// <exception cref="PipelineStoppedException"/>
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

	/// <inheritdoc cref="SonarrCmdletBase" path="/provider"/>
	/// <inheritdoc cref="Cmdlet.ProcessRecord" path="/*[not(self::summary)]"/>
	/// <summary>
	/// When overridden in the derived <see cref="SonarrCmdletBase"/> cmdlet, performs clean-up 
	/// of the command after execution using the provided <see cref="IServiceProvider"/> of the 
	/// scope cmdlet. Default implementation in the base class just returns.
	/// </summary>
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
	/// This method cannot be overriden. Derived cmdlets should instead override the
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
	/// When overridden in the derived <see cref="SonarrCmdletBase"/> cmdlet, signals the code that runs in the
	/// command to stop, such as by canceling the token of a request in flight. Default implementation in the base class
	/// just returns.
	/// </summary>
	/// <remarks>
	/// It runs on another thread while <see cref="Begin(IServiceProvider)"/>, <see cref="Process(IServiceProvider)"/>,
	/// or <see cref="End(IServiceProvider)"/> can still be running, and while the pipeline thread can be disposing the
	/// scope, so it only signals. Whatever it throws is lost.
	/// </remarks>
	/// <param name="provider">The scoped service provider for use by derived cmdlets.</param>
	[DebuggerStepThrough]
	protected virtual void Stop(IServiceProvider provider)
	{
		return;
	}

	/// <summary>
	/// Sets the internal "IsStopped" flag in the base cmdlet.
	/// </summary>
	/// <remarks>This flag is checked prior to the execution
	/// of the <see cref="Begin(IServiceProvider)"/>, <see cref="Process(IServiceProvider)"/>, and
	/// <see cref="End(IServiceProvider)"/> methods in order to determine is the next processing step 
	/// should be skipped.
	/// </remarks>
	[DebuggerStepThrough]
	protected void StopCmdlet()
	{
		_isStopped = true;
		this.OnErrorStopping(_scope!.ServiceProvider, null, errorWasProvided: false);
	}
	/// <summary>
	/// Sets the internal "IsStopped" flag in the base cmdlet and writes the supplied 
	/// <see cref="ErrorRecord"/> to the output pipeline which can be suppressed via the "ErrorAction" 
	/// preference values.
	/// </summary>
	/// <remarks>This flag is checked prior to the execution
	/// of the <see cref="Begin(IServiceProvider)"/>, <see cref="Process(IServiceProvider)"/>, and
	/// <see cref="End(IServiceProvider)"/> methods in order to determine is the next processing step 
	/// should be skipped.
	/// </remarks>
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
	/// When overridden in derived cmdlets, it sets any logic that should be run before 
	/// <see cref="Begin(IServiceProvider)"/> is executed but immediately after the scoped
	/// service provider is created. Default implementation in the base class just returns.
	/// </summary>
	/// <remarks>
	///     This method should be used in lieu of setting member fields/properties in the cmdlet's
	///     constructor.
	///     <para>
	///         Any exception thrown will stop the execution of the cmdlet entirely.
	///     </para>
	/// </remarks>
	/// <param name="provider">The scoped service provider for use by derived cmdlets.</param>
	/// <exception cref="Exception"/>
	[DebuggerStepThrough]
	protected virtual void OnCreatingScope(IServiceProvider provider)
	{
		return;
	}

	private protected virtual void OnCreatingScopeInternal(IServiceProvider provider)
	{
		return;
	}

	/// <summary>
	/// When overriden in derived cmdlets, will run any logic when the <see cref="StopCmdlet()"/> methods
	/// are called but before any errors/exceptions are thrown. In the case that <see cref="StopCmdlet(ErrorRecord)"/>
	/// was called, the <see cref="ErrorRecord"/> is passed on to this method; otherwise it will be 
	/// <see langword="null"/>.
	/// </summary>
	/// <remarks>
	///     Default implementation in the base class just returns.
	/// </remarks>
	/// <param name="provider"></param>
	[DebuggerStepThrough]
	protected virtual void OnErrorStopping(IServiceProvider provider, [ParameterNotNullWhenTrue(nameof(errorWasProvided))] ErrorRecord? error, bool errorWasProvided)
	{
		return;
	}
}

