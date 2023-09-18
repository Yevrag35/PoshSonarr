using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Shell.Context;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets
{
    public abstract class SonarrCmdletBase : PSCmdlet
    {
        protected ActionPreference DebugPreference { get; private set; }
        IServiceScope Scope { get; }
        protected internal IServiceProvider Services => this.Scope.ServiceProvider;
        protected ActionPreference VerbosePreference { get; private set; }

        protected SonarrCmdletBase()
        {
            this.Scope = this.CreateScope();
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            this.StoreVerbosePreference();
            this.StoreDebugPreference();
        }

        protected override void EndProcessing()
        {
            this.Scope.Dispose();
        }
        protected override void StopProcessing()
        {
            this.Scope.Dispose();
        }

        private void StoreDebugPreference()
        {
            if (this.MyInvocation.BoundParameters.TryGetValue(Constants.DEBUG, out object? oVal)
                            &&
                            ((oVal is SwitchParameter sw && sw.ToBool()) || (oVal is bool justBool && justBool)))
            {
                this.DebugPreference = ActionPreference.Continue;
            }
            else if (this.SessionState.PSVariable.TryGetVariableValue(Constants.VERBOSE_PREFERENCE, out ActionPreference pref))
            {
                this.DebugPreference = pref;
            }
        }
        private void StoreVerbosePreference()
        {
            if (this.MyInvocation.BoundParameters.TryGetValue(Constants.VERBOSE, out object? oVal)
                            &&
                            ((oVal is SwitchParameter sw && sw.ToBool()) || (oVal is bool justBool && justBool)))
            {
                this.VerbosePreference = ActionPreference.Continue;
            }
            else if (this.SessionState.PSVariable.TryGetVariableValue(Constants.VERBOSE_PREFERENCE, out ActionPreference pref))
            {
                this.VerbosePreference = pref;
            }
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
        protected void WriteVerboseSonarrResult(ISonarrResponse response, JsonSerializerOptions? options = null)
        {
            if (this.VerbosePreference != ActionPreference.SilentlyContinue)
            {
                options ??= this.Services.GetService<SonarrJsonOptions>()?.GetForSerializing();
                this.WriteVerbose(JsonSerializer.Serialize(response, options));
            }
        }
    }
}
