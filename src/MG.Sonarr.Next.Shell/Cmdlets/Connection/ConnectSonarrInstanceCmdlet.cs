using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Exceptions;
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

        [Parameter(Mandatory = true, Position = 1)]
        [Alias("Key")]
        [ValidateNotNullOrEmpty]
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
            this.ValidateSettings(_settings);

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
                this.UnsetContext();
                this.ThrowTerminatingError(result.Error);
            }
        }

        /// <exception cref="SonarrParameterException"/>
        private void ValidateSettings(ConnectionSettings settings)
        {
            ParameterErrorType type = ParameterErrorType.Invalid;

            try
            {
                settings.Validate();
            }
            catch (InvalidApiKeyException keyEx)
            {
                string? key = settings.Key.GetValue();
                if (string.IsNullOrEmpty(key))
                {
                    type = ParameterErrorType.Missing;
                }
                else
                {
                    type |= ParameterErrorType.Malformed;
                }

                SonarrParameterException pEx = new(nameof(this.ApiKey), type, null, keyEx);
                this.ThrowTerminatingError(pEx.ToRecord());
            }
            catch (ArgumentNullException nullEx)
            {
                type |= ParameterErrorType.Malformed;
                SonarrParameterException pEx = new(nameof(this.Url), type, null, nullEx);
                this.ThrowTerminatingError(pEx.ToRecord(settings.ServiceUri));
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
            this.WriteVerbose($"Sending {request.RequestMethod}  request ->  {request.RequestUrl}");
        }
        public void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options = null)
        {
            if (this.VerbosePreference != ActionPreference.SilentlyContinue)
            {
                options ??= provider.GetService<ISonarrJsonOptions>()?.ForSerializing;
                this.WriteVerbose(JsonSerializer.Serialize(response, options));
            }
        }
    }
}
