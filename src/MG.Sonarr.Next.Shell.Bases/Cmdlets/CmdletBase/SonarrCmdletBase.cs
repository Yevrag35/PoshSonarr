using MG.Sonarr.Next.Attributes;
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
    public abstract partial class SonarrCmdletBase : PSCmdlet,
        IDisposable,
        IIsRunning<SonarrCmdletBase>,
        IScopeCmdlet<SonarrCmdletBase>
    {
        private bool _disposed;
        private ErrorRecord? _error;
        private bool _isInitialized;
        private bool _isStopped;
        private IServiceScope? _scope;

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

        #region EXCEPTIONS

        const string PIPELINE_STOPPED = "The pipeline has been stopped.";

        /// <typeparam name="T">The output type of this method. It will never be returned.</typeparam>
        /// <exception cref="PipelineStoppedException"></exception>
        [DoesNotReturn]
        protected static T ThrowPipelineStopped<T>()
        {
            throw new PipelineStoppedException();
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
