using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Settings;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets.Connection
{
    [Cmdlet(VerbsCommunications.Connect, "SonarrInstance")]
    [Alias("Connect-Sonarr")]
    public sealed class ConnectSonarrInstanceCmdlet : PSCmdlet, IApiCmdlet
    {
        ConnectionSettings _settings = null!;
        private ConnectionSettings Settings => _settings;

        private ActionPreference VerbosePreference { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        [Alias("Key")]
        public ApiKey ApiKey
        {
            get => _settings?.Key ?? ApiKey.Empty;
            set => this.SetConnectionSetting(value, (x, settings) => settings.Key = x);
        }

        [Parameter(Mandatory = true, Position = 0)]
        [Alias("SonarrUrl", "Uri")] // for backward compatibility
        [ValidateUrl(UriKind.Absolute)]
        [MaybeNull]
        public Uri Url
        {
            get => _settings?.ServiceUri;
            set => this.SetConnectionSetting(value, (x, settings) => settings.ServiceUri = x);
        }

        [Parameter]
        [Alias("NoApiPrefix")]  // for backward-compatibility
        public SwitchParameter NoApiInPath
        {
            get => _settings?.NoApiInPath ?? default;
            set => this.SetConnectionSetting(value.ToBool(), (x, settings) => settings.NoApiInPath = x);
        }

        [Parameter]
        public SwitchParameter SkipCertificateCheck
        {
            get => _settings?.SkipCertValidation ?? default;
            set => this.SetConnectionSetting(value.ToBool(), (x, settings) => settings.SkipCertValidation = x);
        }

        [Parameter]
        [PSDefaultValue(Value = "5 minutes")]
        public TimeSpan Timeout
        {
            get => _settings?.Timeout ?? TimeSpan.Zero;
            set => this.SetConnectionSetting(value, (x, settings) => settings.Timeout = x);
        }

        protected override void BeginProcessing()
        {
            _settings ??= new();
            this.StoreVerbosePreference();
        }
        protected override void ProcessRecord()
        {
            if (_settings.Timeout <= TimeSpan.Zero)
            {
                _settings.Timeout = TimeSpan.FromMinutes(5d);
            }

            this.SetContext(this.Settings, (services, options) =>
            {
                return services.BuildServiceProvider(options);
            });
            using IServiceScope scope = this.CreateScope();

            var queue = scope.ServiceProvider.GetService<Queue<IApiCmdlet>>();
            queue?.Enqueue(this);
            var client = scope.ServiceProvider.GetRequiredService<ISonarrClient>();
            var result = client.SendTest();

            if (result.IsError)
            {
                this.ThrowTerminatingError(result.Error);
            }
        }
        protected override void EndProcessing()
        {
            _settings = null!;
        }

        private void SetConnectionSetting<T>(T? value, Action<T, ConnectionSettings> setValue)
        {
            if (value is not null)
            {
                _settings ??= new();
                setValue.Invoke(value, _settings);
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
        public void WriteVerboseBefore(IHttpRequestDetails request)
        {
            this.WriteVerbose($"Sending {request.Method}  request ->  {request.RequestUri}");
        }
        public void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options = null)
        {
            if (this.VerbosePreference != ActionPreference.SilentlyContinue)
            {
                options ??= provider.GetService<SonarrJsonOptions>()?.GetForSerializing();
                this.WriteVerbose(JsonSerializer.Serialize(response, options));
            }
        }
    }
}
