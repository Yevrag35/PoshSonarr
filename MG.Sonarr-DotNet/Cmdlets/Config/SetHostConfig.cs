using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Set, "HostConfig", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [Alias("Update-HostConfig")]
    [OutputType(typeof(UIHost))]
    [CmdletBinding(PositionalBinding = false)]
    public class SetHostConfig : GetHostConfig
    {
        #region FIELDS/CONSTANTS
        private bool _analyticsEnabled;
        private bool _launchBrowser;
        private bool _proxyBypass;
        private bool _proxyEnabled;
        private bool _sslEnabled;
        private bool _updateAuto;
        private bool _passThru;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, ValueFromPipeline = true)]
        public UIHost InputObject { get; set; }

        [Parameter(Mandatory = false)]
        public string BindAddress { get; set; }

        [Parameter(Mandatory = false)]
        public int Port { get; set; }

        [Parameter(Mandatory = false)]
        public int SslPort { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter SslEnabled
        {
            get => _sslEnabled;
            set => _sslEnabled = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter LaunchBrowserAtStartup
        {
            get => _launchBrowser;
            set => _launchBrowser = value;
        }

        [Parameter(Mandatory = false)]
        public AuthenticationType AuthenticationMethod { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter AnalyticsEnabled
        {
            get => _analyticsEnabled;
            set => _analyticsEnabled = value;
        }

        [Parameter(Mandatory = false)]
        public PSCredential NewCredential { get; set; }

        [Parameter(Mandatory = false)]
        public LogLevel LogLevel { get; set; }

        [Parameter(Mandatory = false, DontShow = true)]
        public string Branch { get; set; }

        //[Parameter(Mandatory = false)]
        //public string UrlBase { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter UpdateAutomatically
        {
            get => _updateAuto;
            set => _updateAuto = value;
        }

        [Parameter(Mandatory = false)]
        public UpdateMechanism UpdateMechanism { get; set; }

        [Parameter(Mandatory = false)]
        public string UpdateScriptPath { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter ProxyEnabled
        {
            get => _proxyEnabled;
            set => _proxyEnabled = value;
        }

        [Parameter(Mandatory = false)]
        public ProxyType ProxyType { get; set; }

        [Parameter(Mandatory = false)]
        public string ProxyHostName { get; set; }

        [Parameter(Mandatory = false)]
        public int ProxyPort { get; set; }

        [Parameter(Mandatory = false)]
        public PSCredential ProxyCredential { get; set; }

        [Parameter(Mandatory = false)]
        public string ProxyBypassFilter { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter ProxyBypassOnLocal
        {
            get => _proxyBypass;
            set => _proxyBypass = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            //if ( ! base.HasParameterSpecified(this, x => x.InputObject))
            if (!this.ContainsParameter(x => x.InputObject))
            {
                this.InputObject = base.GetUIHost();
            }
        }

        protected override void EndProcessing()
        {
            if (this.InputObject != null && base.ShouldProcess("HostConfig", "Set"))
            {
                this.ModifyBasedOnParameters();
                UIHost modifiedHost = base.SendSonarrPut<UIHost>(EP, this.InputObject);
                if (_passThru)
                    base.SendToPipeline(modifiedHost);
            }
        }

        #endregion

        #region BACKEND METHODS
        private void ModifyBasedOnParameters()
        {
            if (this.ContainsAnyParameters(x => x.BindAddress, x => x.Branch, x => x.AnalyticsEnabled, x => x.LaunchBrowserAtStartup, x => x.LogLevel,
                x => x.NewCredential, x => x.Port, x => x.ProxyBypassFilter, x => x.ProxyBypassOnLocal, x => x.ProxyCredential, x => x.ProxyEnabled,
                x => x.ProxyHostName, x => x.ProxyPort, x => x.ProxyType, x => x.SslEnabled, x => x.SslPort, x => x.UpdateAutomatically,
                x => x.UpdateMechanism, x => x.UpdateScriptPath, x => x.AuthenticationMethod))
            {
                if (this.ContainsParameter(x => x.AuthenticationMethod))
                    this.InputObject.AuthenticationMethod = this.AuthenticationMethod;

                if (this.ContainsParameter(x => x.AnalyticsEnabled))
                    this.InputObject.AnalyticsEnabled = _analyticsEnabled;

                if (this.ContainsParameter(x => x.BindAddress))
                    this.InputObject.BindAddress = this.BindAddress;

                if (this.ContainsParameter(x => x.Branch))
                    this.InputObject.Branch = this.Branch;

                if (this.ContainsParameter(x => x.LaunchBrowserAtStartup))
                    this.InputObject.LaunchBrowser = _launchBrowser;

                if (this.ContainsParameter(x => x.LogLevel))
                    this.InputObject.LogLevel = this.LogLevel;

                if (this.ContainsParameter(x => x.NewCredential))
                {
                    this.InputObject.Username = this.NewCredential.UserName;
                    if (this.NewCredential.Password != null)
                        this.InputObject.Password = this.NewCredential.GetNetworkCredential().Password;
                }
                if (this.ContainsParameter(x => x.Port))
                    this.InputObject.Port = this.Port;

                if (this.ContainsParameter(x => x.ProxyBypassFilter))
                    this.InputObject.ProxyBypassFilter = this.ProxyBypassFilter;

                if (this.ContainsParameter(x => x.ProxyBypassOnLocal))
                    this.InputObject.ProxyBypassLocalAddresses = _proxyBypass;

                if (this.ContainsParameter(x => x.ProxyCredential))
                {
                    this.InputObject.ProxyUsername = this.ProxyCredential.UserName;
                    if (this.ProxyCredential.Password != null)
                        this.InputObject.ProxyPassword = this.ProxyCredential.GetNetworkCredential().Password;
                }
                if (this.ContainsParameter(x => x.ProxyEnabled))
                    this.InputObject.ProxyEnabled = _proxyEnabled;

                if (this.ContainsParameter(x => x.ProxyHostName))
                    this.InputObject.ProxyHostname = this.ProxyHostName;

                if (this.ContainsParameter(x => x.ProxyPort))
                    this.InputObject.ProxyPort = this.ProxyPort;

                if (this.ContainsParameter(x => x.ProxyType))
                    this.InputObject.ProxyType = this.ProxyType;

                if (this.ContainsParameter(x => x.SslEnabled))
                    this.InputObject.EnableSsl = _sslEnabled;

                if (this.ContainsParameter(x => x.SslPort))
                    this.InputObject.SslPort = this.SslPort;

                if (this.ContainsParameter(x => x.UpdateAutomatically))
                    this.InputObject.UpdateAutomatically = _updateAuto;

                if (this.ContainsParameter(x => x.UpdateMechanism))
                    this.InputObject.UpdateMechanism = this.UpdateMechanism;

                if (this.ContainsParameter(x => x.UpdateScriptPath))
                    this.InputObject.UpdateScriptPath = this.UpdateScriptPath;

                //if (base.HasParameterSpecified(this, x => x.UrlBase))
                //    this.InputObject.UrlBase = this.UrlBase;
            }
        }

        #endregion
    }
}