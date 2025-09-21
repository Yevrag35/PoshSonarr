using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Exceptions;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Settings;
using System.Text.Json;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Shell.Services;
using MG.Sonarr.Next.Services.Jobs;
using MG.Sonarr.Resources;
using System.Management.Automation.Host;

namespace MG.Sonarr.Next.Shell.Cmdlets.Connection
{
    [Cmdlet(VerbsCommunications.Connect, "SonarrInstance")]
    [Alias("Connect-Sonarr")]
    public sealed class ConnectSonarrInstanceCmdlet : ConnectCmdlet, IApiCmdlet
    {
        ConnectionSettings _settings = null!;

        private ActionPreference _debugPreference;
        private ActionPreference _verbosePreference;

        public bool CanDebugSerializeAfter => _debugPreference != ActionPreference.SilentlyContinue;
        public bool CanDebugSerializeBefore => _debugPreference != ActionPreference.SilentlyContinue;

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
        public SwitchParameter PassThru { get; set; }

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

        public bool CanWriteVerbose => _verbosePreference is not (ActionPreference.SilentlyContinue or ActionPreference.Ignore);

        protected override void BeginProcessing()
        {
            _settings ??= new();
            this.StorePreferences();
        }
        protected override void ProcessRecord()
        {
            this.ValidateSettings(_settings);

            if (_settings.Timeout <= TimeSpan.Zero)
            {
                _settings.Timeout = TimeSpan.FromMinutes(5);
            }

            using IServiceScope scope = this.ConnectContext(ModuleServiceConfigurer.AddConfiguration);

            var queue = scope.ServiceProvider.GetService<ApiCmdletQueue>();
            queue?.Enqueue(this);
            var client = scope.ServiceProvider.GetRequiredService<ISonarrClient>();

            ISonarrResponse result = this.SendTest(client, scope.ServiceProvider, this.PassThru);

            if (result.IsError)
            {
                this.DisconnectContext();
                this.ThrowTerminatingError(result.Error);
            }
        }

        private ISonarrResponse SendTest(ISonarrClient client, IServiceProvider provider, bool passThru)
        {
            if (!passThru)
            {
                return client.SendTest();
            }

            var tag = provider.GetMetadataTag(Meta.STATUS);
            var response = client.SendGetAsync<SystemStatusObject>(tag.UrlBase).GetAwaiter().GetResult();
            if (!response.IsError)
            {
                var settings = provider.GetRequiredService<IConnectionSettings>();
                settings.AuthType = response.Value.Authentication;

                this.WriteObject(response.Value);
            }

            return response;
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

        protected override IConnectionSettings GetConnectionSettings()
        {
            return _settings;
        }

        private bool IsVerboseNotSilentAndUICanWrite([NotNullWhen(true)] out PSHostUserInterface? hostInterface)
        {
            hostInterface = this.Host?.UI;
            return this.CanWriteVerbose && hostInterface is not null;
        }
        private bool IsDebugNotSilentAndUICanWrite([NotNullWhen(true)] out PSHostUserInterface? hostInterface)
        {
            hostInterface = this.Host?.UI;
            return _debugPreference is not (ActionPreference.SilentlyContinue or ActionPreference.Ignore) && hostInterface is not null;
        }

        private void SetConnectionSetting<T>(T? value, Action<T, ConnectionSettings> setValue)
        {
            if (value is not null)
            {
                _settings ??= new();
                setValue.Invoke(value, _settings);
            }
        }

        private void StorePreferences()
        {
            _verbosePreference = this.GetActionPreferenceFromSwitch(PSConstants.VERBOSE, PSConstants.VERBOSE_PREFERENCE);
            _debugPreference = this.GetActionPreferenceFromSwitch(PSConstants.DEBUG, PSConstants.DEBUG_PREFERENCE);
        }
        public void WriteDebugPayload(string jsonPayload)
        {
            if (this.IsDebugNotSilentAndUICanWrite(out PSHostUserInterface? hostInterface))
            {
                hostInterface.WriteDebugLine(jsonPayload);
            }
        }
        public void WriteVerboseBefore(IHttpRequestDetails request)
        {
            if (this.IsVerboseNotSilentAndUICanWrite(out PSHostUserInterface? hostUI))
            {
                string msg = Messenger.Format(
                    format: Messages.Verbose_SendingRequest_Format,
                    [request.RequestMethod, request.RequestUrl]);

                hostUI.WriteVerboseLine(msg);
            }
        }
        public void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options = null)
        {
            if (this.IsVerboseNotSilentAndUICanWrite(out PSHostUserInterface? hostUI))
            {
                options ??= provider.GetService<ISonarrJsonOptions>()?.ForSerializing;
                string json = JsonSerializer.Serialize(response, options);
                hostUI.WriteVerboseLine(json);
            }
        }
    }
}
