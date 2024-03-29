﻿using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Exceptions;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Settings;
using System.Text.Json;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Collections.Pools;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;

namespace MG.Sonarr.Next.Shell.Cmdlets.Connection
{
    [Cmdlet(VerbsCommunications.Connect, "SonarrInstance")]
    [Alias("Connect-Sonarr")]
    public sealed class ConnectSonarrInstanceCmdlet : ConnectCmdlet, IApiCmdlet
    {
        ConnectionSettings _settings = null!;

        private ActionPreference _verbosePreference;

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

            using IServiceScope scope = this.ConnectContext(ConfigureServices);

            var queue = scope.ServiceProvider.GetService<Queue<IApiCmdlet>>();
            queue?.Enqueue(this);
            var client = scope.ServiceProvider.GetRequiredService<ISonarrClient>();

            ISonarrResponse result = this.SendTest(client, scope.ServiceProvider, this.PassThru);

            if (result.IsError)
            {
                this.DisconnectContext();
                this.ThrowTerminatingError(result.Error);
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<ManualImportEdit>()
                    .AddScoped<ReleaseProfileObject>()
                    .AddGenericObjectPool<Dictionary<int, IEpisodeBySeriesPipeable>>(builder =>
                    {
                        builder.SetConstructor(() => new Dictionary<int, IEpisodeBySeriesPipeable>(50))
                               .SetDeconstructor(dict =>
                               {
                                   dict.Clear();
                                   int cap = dict.EnsureCapacity(50);
                                   if (cap >= 1000)
                                   {
                                       dict.TrimExcess(50);
                                   }

                                   return true;
                               });
                    })
                    .AddGenericObjectPool<HashSet<DayOfWeek>>(set =>
                    {
                        int count = set.Count;
                        set.Clear();
                        return count <= 1000;
                    })
                    .AddGenericObjectPool<SortedSet<SonarrProperty>>(set =>
                    {
                        int count = set.Count;
                        set.Clear();

                        return count <= 3000;
                    });
        }

        private ISonarrResponse SendTest(ISonarrClient client, IServiceProvider provider, bool passThru)
        {
            if (!passThru)
            {
                return client.SendTest();
            }

            var tag = provider.GetMetadataTag(Meta.STATUS);
            var response = client.SendGet<SystemStatusObject>(tag.UrlBase);
            if (!response.IsError)
            {
                var settings = provider.GetRequiredService<IConnectionSettings>();
                settings.AuthType = response.Data.Authentication;

                this.WriteObject(response.Data);
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
            if (this.MyInvocation.BoundParameters.TryGetValue(PSConstants.VERBOSE, out object? oVal)
                            &&
              ((oVal is SwitchParameter sw && sw.ToBool()) || (oVal is bool justBool && justBool)))
            {
                _verbosePreference = ActionPreference.Continue;
            }
            else if (this.SessionState.PSVariable.TryGetVariableValue(PSConstants.VERBOSE_PREFERENCE, out ActionPreference pref))
            {
                _verbosePreference = pref;
            }
        }
        public void WriteVerboseBefore(IHttpRequestDetails request)
        {
            this.WriteVerbose($"Sending {request.RequestMethod}  request ->  {request.RequestUrl}");
        }
        public void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options = null)
        {
            if (_verbosePreference != ActionPreference.SilentlyContinue)
            {
                options ??= provider.GetService<ISonarrJsonOptions>()?.ForSerializing;
                this.WriteVerbose(JsonSerializer.Serialize(response, options));
            }
        }
    }
}
