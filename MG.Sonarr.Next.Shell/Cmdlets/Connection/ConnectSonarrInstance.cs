﻿using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Context;
using MG.Sonarr.Next.Shell.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Connection
{
    [Cmdlet(VerbsCommunications.Connect, "SonarrInstance")]
    public sealed class ConnectSonarrInstance : Cmdlet
    {
        CancellationTokenSource _token = null!;
        internal ConnectionSettings Settings { get; }

        public ConnectSonarrInstance()
        {
            this.Settings = new();
        }

        [Parameter(Mandatory = true, Position = 1)]
        public ApiKey ApiKey
        {
            get => this.Settings.Key;
            set => this.Settings.Key = value;
        }

        [Parameter(Mandatory = true, Position = 0)]
        public Uri Url
        {
            get => this.Settings.ServiceUri;
            set => this.Settings.ServiceUri = value;
        }

        [Parameter(Mandatory = false)]
        [Alias("NoApiPrefix")]
        public SwitchParameter NoApiInPath
        {
            get => this.Settings.NoApiInPath;
            set => this.Settings.NoApiInPath = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter SkipCertificateCheck
        {
            get => this.Settings.SkipCertValidation;
            set => this.Settings.SkipCertValidation = value;
        }

        protected override void BeginProcessing()
        {
            _token = new CancellationTokenSource();
        }
        protected override void ProcessRecord()
        {
            this.SetContext();
            var provider = this.GetServiceProvider();
            using var scope = provider.CreateScope();

            var client = scope.ServiceProvider.GetRequiredService<ISonarrClient>();
            var result = client.SendGet<PSObject>("api/system/status", _token.Token).GetAwaiter().GetResult();

            if (result.IsError)
            {
                this.WriteError(result.Error);
            }
            else
            {
                this.WriteObject(result.Data);
            } 
        }

        protected override void EndProcessing()
        {
            _token.Dispose();
        }
        protected override void StopProcessing()
        {
            _token.Cancel();
            base.StopProcessing();
            _token.Dispose();
        }
    }
}
