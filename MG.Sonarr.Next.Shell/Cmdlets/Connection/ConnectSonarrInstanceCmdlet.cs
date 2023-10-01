using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Settings;
using Microsoft.Extensions.DependencyInjection;
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

        public ConnectSonarrInstanceCmdlet()
        {
        }

        [Parameter(Mandatory = true, Position = 1)]
        [Alias("Key")]
        public ApiKey ApiKey
        {
            get => this.Settings.Key;
            set
            {
                _settings ??= new();
                this.Settings.Key = value;
            }
        }

        [Parameter(Mandatory = true, Position = 0)]
        [Alias("SonarrUrl")] // for backward compatibility
        public Uri Url
        {
            get => this.Settings.ServiceUri;
            set
            {
                _settings ??= new();
                this.Settings.ServiceUri = value;
            }
        }

        [Parameter(Mandatory = false)]
        [Alias("NoApiPrefix")]  // for backward-compatibility
        public SwitchParameter NoApiInPath
        {
            get => this.Settings.NoApiInPath;
            set
            {
                _settings ??= new();
                this.Settings.NoApiInPath = value.ToBool();
            }
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter SkipCertificateCheck
        {
            get => this.Settings.SkipCertValidation;
            set
            {
                _settings ??= new();
                this.Settings.SkipCertValidation = value;
            }
        }

        protected override void BeginProcessing()
        {
            _settings = new();
            this.StoreVerbosePreference();
        }
        protected override void ProcessRecord()
        {
            this.SetContext(this.Settings, (services, options) =>
            {
                return services.BuildServiceProvider(options);
            });
            IServiceProvider provider = this.GetServiceProvider();
            using var scope = provider.CreateScope();

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

        public void WriteVerbose(HttpRequestMessage request)
        {
            this.WriteVerbose($"Sending {request.Method.Method} request -> {request.RequestUri?.ToString()}");
        }

        public void WriteVerboseSonarrResult(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options = null)
        {
            if (this.VerbosePreference != ActionPreference.SilentlyContinue)
            {
                options ??= provider.GetService<SonarrJsonOptions>()?.GetForSerializing();
                this.WriteVerbose(JsonSerializer.Serialize(response, options));
            }
        }
    }
}
