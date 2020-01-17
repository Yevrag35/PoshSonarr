using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "PlexNotification", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true, DefaultParameterSetName = "ByPSCredential")]
    [OutputType(typeof(Notification))]
    [Alias("New-PlexConnection")]
    [CmdletBinding(PositionalBinding = false)]
    public class NewPlexNotification : NotificationCmdlet
    {
        #region FIELDS/CONSTANTS
        //private bool _force;
        private bool _notDown;
        private bool _notRen;
        private bool _notUpg;
        private bool _upLib;
        private bool _useSsl;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0)]
        [Alias("Name")]
        public string ConnectionName { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        [Alias("HostName")]
        public string PlexServerName { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByPSCredential")]
        public PSCredential PlexCredential { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByPlainUserPass")]
        public string UserName { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByPlainUserPass")]
        public string Password { get; set; }

        [Parameter(Mandatory = false, Position = 2)]
        public int Port = 32400;

        [Parameter(Mandatory = false)]
        public string[] Tags { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter NotifyOnDownload
        {
            get => _notDown;
            set => _notDown = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter NotifyOnRename
        {
            get => _notRen;
            set => _notRen = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter NotifyOnUpgrade
        {
            get => _notUpg;
            set => _notUpg = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter UpdateLibrary
        {
            get => _upLib;
            set => _upLib = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter UseSSL
        {
            get => _useSsl;
            set => _useSsl = value;
        }

        //[Parameter(Mandatory = false)]
        //public SwitchParameter Force
        //{
        //    get => _force;
        //    set => _force = value;
        //}

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

        }

        protected override void ProcessRecord()
        {
            if (base.ShouldProcess("Plex Notification Connection", "New"))
            {
                PlexNotification notif = null;
                if (base.HasParameterSpecified(this, x => x.PlexCredential))
                    notif = this.Create(this.ConnectionName, this.PlexServerName, this.Port, this.PlexCredential, _upLib, _useSsl);

                else
                    notif = this.Create(this.ConnectionName, this.PlexServerName, this.Port, this.UserName, this.Password, _upLib, _useSsl);

                this.SetNotificationOptions(notif);
                base.WriteDebug(notif.ToJson());
                Notification createdNotif = base.SendSonarrPost<Notification>(base.Endpoint, notif);
                base.SendToPipeline(createdNotif);
            }
        }

        #endregion

        #region BACKEND METHODS

        private PlexNotification Create(string name, string host, int port, PSCredential psCreds, bool updateLibrary, bool useSsl)
        {
            return new PlexNotification(host, port, psCreds.UserName, psCreds.GetNetworkCredential().Password, updateLibrary, useSsl)
            {
                Name = name
            };
        }
        private PlexNotification Create(string name, string host, int port, string username, string password, bool updateLibrary, bool useSsl)
        {
            return new PlexNotification(host, port, username, password, updateLibrary, useSsl)
            {
                Name = name
            };
        }

        private void SetNotificationOptions(PlexNotification notif)
        {
            if (this.HasParameterSpecified(this, x => x.NotifyOnDownload))
                notif.OnDownload = _notDown;

            if (this.HasParameterSpecified(this, x => x.NotifyOnRename))
                notif.OnRename = _notRen;

            if (this.HasParameterSpecified(this, x => x.NotifyOnUpgrade))
                notif.OnUpgrade = _notUpg;

            if (this.HasParameterSpecified(this, x => x.UpdateLibrary))
                notif.Fields["UpdateLibrary"].Value = _upLib;

            if (this.HasParameterSpecified(this, x => x.UseSSL))
                notif.Fields["UseSsl"].Value = _useSsl;
        }

        #endregion
    }
}