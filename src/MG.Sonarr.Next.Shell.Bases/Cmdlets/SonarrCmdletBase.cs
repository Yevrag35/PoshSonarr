﻿using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Context;
using MG.Sonarr.Next.Shell.Extensions;
using System.Collections;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets
{
    /// <summary>
    /// The base class for all PoshSonarr cmdlets that query or manipulate data from a Sonarr PVR instance.
    /// 
    /// <para>
    /// When the cmdlet starts the <see cref="BeginProcessing"/> method, an <see cref="IServiceScope"/> is 
    /// created and used for the entire duration of the cmdlet's execution.  At the end of 
    /// <see cref="EndProcessing"/> or <see cref="StopProcessing"/>, the scope will be disposed of.
    /// </para>
    /// </summary>
    /// <remarks>
    ///     It implements <see cref="IDisposable"/> only for visibility.  This and derived classes will manage
    ///     their own resources.
    /// </remarks>
    //[DebuggerStepThrough]
    public abstract class SonarrCmdletBase : PSCmdlet,
        IDisposable,
        IIsRunning<SonarrCmdletBase>,
        IScopeCmdlet<SonarrCmdletBase>
    {
        static readonly Type _enumerableType = typeof(IEnumerable);
        static readonly Type _stringType = typeof(string);

        private static bool _hasChecked;
        static bool IScopeCmdlet<SonarrCmdletBase>.HasChecked()
        {
            return _hasChecked;
        }

        static void IScopeCmdlet<SonarrCmdletBase>.SetChecked(bool toggle)
        {
            _hasChecked = toggle;
        }

        bool _disposed;
        ErrorRecord? _error;
        bool _isInitialized;
        IServiceScope? _scope;
        bool _isStopped;

        [MemberNotNullWhen(true, nameof(Services), nameof(_scope))]
        protected private bool IsScopeInitialized => _isInitialized;
        protected private IServiceProvider? Services => _scope?.ServiceProvider;

        /// <summary>
        /// Specifies whether derived cmdlets should capture the current Debug <see cref="ActionPreference"/> value
        /// before cmdlet processing begins.
        /// </summary>
        /// <remarks>
        ///     Default implementation in the base class always returns <see langword="false"/>.
        /// </remarks>
        protected virtual bool CaptureDebugPreference { get; }

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
        /// Any error that the current cmdlet has experienced.
        /// </summary>
        /// <remarks>
        ///     If this is overwritten (i.e. - set multiple times), it will add the
        ///     previous record as an inner exception.
        /// </remarks>
        protected ErrorRecord? Error
        {
            [DebuggerStepThrough]
            get => _error;

            [DebuggerStepThrough]
            set
            {
                if (value is not null)
                {
                    if (this.HasError)
                    {
                        value = new(_error, value.Exception);
                    }

                    _error = value;
                    this.HasError = true;
                }
            }
        }
        /// <summary>
        /// The current error preference either set from the "-ErrorAction" parameter
        /// or read from the global "$ErrorActionPreference" variable.
        /// </summary>
        /// <remarks>
        ///     This value is set at the very beginning of the <see cref="BeginProcessing"/> execution.
        /// </remarks>
        protected ActionPreference ErrorPreference { get; private set; }
        [MemberNotNullWhen(true, nameof(Error), nameof(_error))]
        bool HasError { get; set; }
        /// <summary>
        /// The current verbosity preference either set from the "-Verbose" <see cref="SwitchParameter"/>
        /// or read from the global "$VerbosePreference" variable.
        /// </summary>
        /// <remarks>
        ///     This value is set at the very beginning of the <see cref="BeginProcessing"/> execution.
        /// </remarks>
        protected ActionPreference VerbosePreference { get; private set; }

        #region PROCESSING

        //[DebuggerStepThrough]
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

        /// <summary>
        /// When overridden in the derived <see cref="SonarrCmdletBase"/> cmdlet, performs initialization of 
        /// command execution using the provided <see cref="IServiceProvider"/> of the scope cmdlet.
        /// Default implementation in the base class just returns.
        /// </summary>
        /// <param name="provider">The scoped service provider for use by derived cmdlets.</param>
        /// <exception cref="Exception"/>
        [DebuggerStepThrough]
        protected virtual void Begin(IServiceProvider provider)
        {
            return;
        }

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
        /// <summary>
        /// When overridden in the derived <see cref="SonarrCmdletBase"/> cmdlet, performs execution of 
        /// the command using the provided <see cref="IServiceProvider"/> of the scope cmdlet.
        /// Default implementation in the base class just returns.
        /// </summary>
        /// <param name="provider">The scoped service provider for use by derived cmdlets.</param>
        /// <exception cref="Exception"/>
        [DebuggerStepThrough]
        protected virtual void Process(IServiceProvider provider)
        {
            return;
        }

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
        /// <summary>
        /// When overridden in the derived <see cref="SonarrCmdletBase"/> cmdlet, performs clean-up 
        /// of the command after execution using the provided <see cref="IServiceProvider"/> of the 
        /// scope cmdlet. Default implementation in the base class just returns.
        /// </summary>
        /// <param name="provider">The scoped service provider for use by derived cmdlets.</param>
        /// <exception cref="Exception"/>
        [DebuggerStepThrough]
        protected virtual void End(IServiceProvider provider)
        {
            return;
        }

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

        #endregion

        private static bool IsEnumerableType(Type type)
        {
            return _enumerableType.IsAssignableFrom(type) && !_stringType.IsAssignableFrom(type);
        }

        /// <summary>
        /// Serializes a given object to the cmdlet's Debug output stream but only if the
        /// <see cref="DebugPreference"/> preference would allow for writing it. This possibly saves not 
        /// calling on expensive serialization operations if the output would not be shared.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The object to serialize.</param>
        /// <param name="message">An optional message to display before the serialization.</param>
        /// <param name="options">Options to provide the <see cref="JsonSerializer"/>.</param>
        [DebuggerStepThrough]
        protected void SerializeIfDebug<T>(T value, string? message = null, JsonSerializerOptions? options = null)
        {
            if (this.DebugPreference != ActionPreference.SilentlyContinue)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    this.WriteDebug(message);
                }

                Type type = value is not null
                    ? typeof(T)
                    : typeof(object);

                this.WriteDebug($"Serializing 'value' of type: {type.FullName ?? type.Name}");
                this.WriteDebug(JsonSerializer.Serialize(value, type, options));
            }
        }

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

        [DebuggerStepThrough]
        protected bool TryWriteObject<T>(in SonarrResponse<T> response)
        {
            return this.TryWriteObject(
                in response,
                writeConditionally: true,
                enumerateCollection: IsEnumerableType(typeof(T)));
        }
        [DebuggerStepThrough]
        protected bool TryWriteObject<T, TOutput>(in SonarrResponse<T> response, Func<T, TOutput?> writeSelector)
        {
            return this.TryWriteObject(in response,
                writeConditionally: true,
                enumerateCollection: IsEnumerableType(typeof(TOutput)),
                writeSelector);
        }
        [DebuggerStepThrough]
        protected bool TryWriteObject<T>(in SonarrResponse<T> response, bool enumerateCollection)
        {
            return this.TryWriteObject(in response, writeConditionally: true, enumerateCollection);
        }
        [DebuggerStepThrough]
        protected bool TryWriteObject<T>(in SonarrResponse<T> response, bool enumerateCollection, Func<T, object?> writeSelector)
        {
            return this.TryWriteObject(in response, writeConditionally: true, enumerateCollection, writeSelector);
        }
        [DebuggerStepThrough]
        protected bool TryWriteObject<T>(in SonarrResponse<T> response, bool writeConditionally, bool enumerateCollection)
        {
            return this.TryWriteObject(in response, writeConditionally, enumerateCollection, x => x);
        }
        protected bool TryWriteObject<T, TOutput>(in SonarrResponse<T> response, bool writeConditionally, bool enumerateCollection, Func<T, TOutput?> writeSelector)
        {
            if (!response.IsError)
            {
                this.WriteObject(writeSelector(response.Data), enumerateCollection);
                return true;
            }
            else if (writeConditionally)
            {
                this.WriteConditionalError(response.Error);
            }
            else
            {
                this.WriteError(response.Error);
            }

            return false;
        }

        /// <summary>
        /// Writes the given error record to the host, but only if 
        /// <see cref="SonarrErrorRecord.IsIgnorable"/> is <see langword="false"/>.
        /// </summary>
        /// <param name="error">The error record to write.</param>
        [DebuggerStepThrough]
        protected void WriteConditionalError(SonarrErrorRecord error)
        {
            if (error.IsIgnorable)
            {
                return;
            }

            this.WriteError(error);
        }

        #region CLEANUP

        protected virtual void Cleanup()
        {
            this.Dispose();
        }

        #region DISPOSAL
        /// <summary>
        /// When overriden in derived classes, it should dispose of any resources when the cmdlet's execution
        /// has finished that may require a new scoped <see cref="IServiceProvider"/>. Default implementation
        /// in the base class just returns.
        /// </summary>
        /// <param name="disposing">The method should dispose of resources.</param>
        /// <param name="scopeFactory">
        ///     The scope factory that can be used to create any services needed.
        /// </param>
        [DebuggerStepThrough]
        protected virtual void Dispose(bool disposing, IServiceScopeFactory? scopeFactory)
        {
            return;
        }

        /// <summary>
        /// The overridable disposal method that cleans up the scoped <see cref="IServiceProvider"/>
        /// instance when the <see cref="SonarrCmdletBase"/> has finished execution.  If a 
        /// <see cref="IServiceProvider"/> is needed in derived cmdlet's context, then the 
        /// <see cref="Dispose(bool, IServiceScopeFactory?)"/> overload should be overriden.
        /// </summary>
        /// <param name="disposing">The method should dispose of the scope.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                IServiceProvider? provider = null;
                try
                {
                    provider = this.CreateScope().ServiceProvider;
                }
                catch (InvalidOperationException)
                {
                    provider = null;
                }

                this.Dispose(disposing, provider?.GetService<IServiceScopeFactory>());

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
}