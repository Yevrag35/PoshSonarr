using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Context;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets
{
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

            this.DebugPreference = this
                .GetCurrentActionPreferenceFromSwitch(PSConstants.DEBUG, PSConstants.DEBUG_PREFERENCE);
        }
        [DebuggerStepThrough]
        private void StoreVerbosePreference(bool wantsCapture)
        {
            if (!wantsCapture)
            {
                return;
            }

            this.VerbosePreference = this
                .GetCurrentActionPreferenceFromSwitch(PSConstants.VERBOSE, PSConstants.VERBOSE_PREFERENCE);
        }

        #endregion

        /// <inheritdoc cref="Cmdlet.BeginProcessing" path="/*[not(self::summary) and not(self::remarks)]"/>
        /// <summary>
        /// Performs initialization of command execution and traps any exceptions thrown.
        /// </summary>
        /// <remarks>
        /// This method cannot be overriden.  Dervied cmdlets should instead override the
        /// <see cref="Begin(IServiceProvider)"/> method.
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
            catch (Exception e)
            {
                this.Error = e.ToRecord();
            }

            if (this.HasError)
            {
                this.Cleanup();
                this.ThrowTerminatingError(this.Error);
                return;
            }
            else if (_isStopped)
            {
                return;
            }

            try
            {
                this.StoreVerbosePreference(this.CaptureVerbosePreference);
                this.StoreDebugPreference(this.CaptureDebugPreference);
                this.ErrorPreference = this
                    .GetCurrentActionPreferenceFromParam(
                        PSConstants.ERROR_ACTION, PSConstants.ERROR_ACTION_PREFERENCE);
            }
            catch (Exception e)
            {
                this.Error = e.ToRecord();
                this.Cleanup();
                return;
            }

            try
            {
                this.Begin(_scope.ServiceProvider);
            }
            catch (PipelineStoppedException)
            {
                Debug.WriteLine("Pipeline stopped.");
                this.Cleanup();
                throw;
            }
            catch (Exception e)
            {
                this.Error = e.ToRecord();
                this.Cleanup();
                return;
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
        /// Performs execution of the command for each input object in the pipeline and traps any 
        /// exceptions thrown.
        /// </summary>
        /// <remarks>
        /// This method cannot be overriden.  Dervied cmdlets should instead override the
        /// <see cref="Process(IServiceProvider)"/> method.
        /// </remarks>
        /// <exception cref="PipelineStoppedException"/>
        [DebuggerStepThrough]
        protected sealed override void ProcessRecord()
        {
            if (_isStopped)
            {
                return;
            }

            _scope ??= this.CreateScope();

            try
            {
                this.Process(_scope.ServiceProvider);
            }
            catch (PipelineStoppedException)
            {
                Debug.WriteLine("Pipeline stopped.");
                this.Cleanup();
                throw;
            }
            catch (Exception e)
            {
                this.Error = e.ToRecord();
                this.Cleanup();
                return;
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
        /// Performs clean-up and disposal of the command after the command execution and traps any 
        /// exceptions thrown.
        /// </summary>
        /// <remarks>
        /// This method cannot be overriden.  Dervied cmdlets should instead override the
        /// <see cref="End(IServiceProvider)"/> method.
        /// </remarks>
        /// <exception cref="PipelineStoppedException"/>
        [DebuggerStepThrough]
        protected sealed override void EndProcessing()
        {
            Queue<IApiCmdlet>? queue = null;
            _scope ??= this.CreateScope();

            try
            {
                if (!_isStopped)
                {
                    this.End(_scope.ServiceProvider);
                }

                queue = _scope?.ServiceProvider.GetService<Queue<IApiCmdlet>>();
                queue?.Clear();
            }
            catch (PipelineStoppedException)
            {
                Debug.WriteLine("Pipeline stopped.");
                throw;
            }
            catch (Exception e)
            {
                this.Error = e.ToRecord();
            }
            finally
            {
                queue?.Clear();
                this.Cleanup();
            }

            if (this.HasError)
            {
                this.WriteError(this.Error);
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
        /// Interrupts currently running code within the <see cref="BeginProcessing"/>, 
        /// <see cref="ProcessRecord"/>, or <see cref="EndProcessing"/> methods. If invoked, the 
        /// <see cref="Cleanup"/> method is called prior to throwing a terminating error.
        /// </summary>
        /// <remarks>
        /// This method cannot be overriden.  Dervied cmdlets should instead override the
        /// <see cref="Stop(IServiceProvider)"/> method.
        /// </remarks>
        /// <exception cref="PipelineStoppedException"/>
        [DebuggerStepThrough]
        protected sealed override void StopProcessing()
        {
            _scope ??= this.CreateScope();

            try
            {
                Queue<IApiCmdlet> queue = _scope.ServiceProvider.GetRequiredService<Queue<IApiCmdlet>>();
                queue.Clear();
                this.Stop(_scope.ServiceProvider);
                this.ThrowTerminatingError(this.Error);
            }
            finally
            {
                this.Cleanup();
            }
        }
        /// <summary>
        /// When overridden in the derived <see cref="SonarrCmdletBase"/> cmdlet, interrupts currently running
        /// code within the command using the provided <see cref="IServiceProvider"/> of the scope cmdlet. 
        /// Default implementation in the base class just returns.
        /// </summary>
        /// <param name="provider">The scoped service provider for use by derived cmdlets.</param>
        /// <exception cref="Exception"/>
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
        protected virtual void OnErrorStopping(IServiceProvider provider, [NotNullWhenTrue(nameof(errorWasProvided))] ErrorRecord? error, bool errorWasProvided)
        {
            return;
        }
    }
}

