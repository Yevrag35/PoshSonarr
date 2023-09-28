using MG.Sonarr.Next.Services.Collections;
using MG.Sonarr.Next.Services.Exceptions;
using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets
{
    public abstract class SonarrCmdletBase : PSCmdlet, IDisposable
    {
        bool _disposed;
        ErrorRecord? _error;
        IServiceScope _scope;
        bool _isStopped;

        protected ActionPreference DebugPreference { get; private set; }
        protected ErrorRecord? Error
        {
            get => _error;
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
        protected ActionPreference ErrorPreference { get; private set; }
        [MemberNotNullWhen(true, nameof(Error), nameof(_error))]
        bool HasError { get; set; }

        protected internal IServiceProvider Services => _scope.ServiceProvider;
        protected ActionPreference VerbosePreference { get; private set; }

        protected SonarrCmdletBase()
        {
            _scope = this.CreateScope();
        }

        protected sealed override void BeginProcessing()
        {
            if (this.HasError)
            {
                this.ThrowTerminatingError(this.Error);
            }
            else if (_isStopped)
            {
                return;
            }

            try
            {
                this.StoreVerbosePreference();
                this.StoreDebugPreference();
                this.ErrorPreference = this
                    .GetCurrentActionPreferenceFromParam(
                        Constants.ERROR_ACTION, Constants.ERROR_ACTION_PREFERENCE);
            }
            catch (Exception e)
            {
                this.Error = e.ToRecord();
                this.Dispose();
                return;
            }

            try
            {
                this.Begin();
            }
            catch (PipelineStoppedException)
            {
                Debug.WriteLine("Pipeline stopped.");
                this.Dispose();
                throw;
            }
            catch (Exception e)
            {
                this.Error = e.ToRecord();
                this.Dispose();
                return;
            }
        }
        protected virtual void Begin()
        {
        }

        protected sealed override void ProcessRecord()
        {
            if (_isStopped)
            {
                return;
            }

            try
            {
                this.Process();
            }
            catch (PipelineStoppedException)
            {
                Debug.WriteLine("Pipeline stopped.");
                this.Dispose();
                throw;
            }
            catch (Exception e)
            {
                this.Error = e.ToRecord();
                this.Dispose();
                return;
            }
        }
        protected virtual void Process()
        {
        }

        protected sealed override void EndProcessing()
        {
            Queue<IApiCmdlet>? queue = null;
            try
            {
                if (!_isStopped)
                {
                    this.End();
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
                this.Dispose();
            }

            if (this.HasError)
            {
                this.WriteConditionalError(this.Error);
            }
        }
        protected virtual void End()
        {
        }

        protected sealed override void StopProcessing()
        {
            try
            {
                Queue<IApiCmdlet> queue = _scope.ServiceProvider.GetRequiredService<Queue<IApiCmdlet>>();
                queue.Clear();
                this.Stop();
                this.ThrowTerminatingError(this.Error);
            }
            finally
            {
                this.Dispose();
            }
        }
        protected virtual void Stop()
        {
        }

        protected void StopCmdlet()
        {
            _isStopped = true;
        }
        protected void StopCmdlet(ErrorRecord record)
        {
            this.WriteError(record);
            this.StopCmdlet();
        }

        protected T GetPooledObject<T>() where T : notnull
        {
            return this.Services.GetRequiredService<IObjectPool<T>>().Get();
        }
        protected void ReturnPooledObject<T>(T item) where T : notnull
        {
            this.Services.GetRequiredService<IObjectPool<T>>().Return(item);
        }

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
        private void StoreDebugPreference()
        {
            this.DebugPreference = this
                .GetCurrentActionPreferenceFromSwitch(Constants.DEBUG, Constants.DEBUG_PREFERENCE);
        }
        private void StoreVerbosePreference()
        {
            this.VerbosePreference = this
                .GetCurrentActionPreferenceFromSwitch(Constants.VERBOSE, Constants.VERBOSE_PREFERENCE);
        }

        protected void WriteConditionalError(ErrorRecord error)
        {
            if (error is SonarrErrorRecord sonarrError && sonarrError.IsIgnorable)
            {
                return;
            }

            this.WriteError(error);
        }
        public void WriteVerboseSonarrResult(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options = null)
        {
            if (this.VerbosePreference != ActionPreference.SilentlyContinue)
            {
                options ??= provider.GetService<SonarrJsonOptions>()?.GetForSerializing();
                this.WriteVerbose(JsonSerializer.Serialize(response, options));
            }
        }

        #region DISPOSAL
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _scope?.Dispose();
                _scope = null!;
                _disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
