using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Results;
using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "Mapping", ConfirmImpact = ConfirmImpact.None, SupportsShouldProcess = true)]
    [OutputType(typeof(RemotePathMapping))]
    public class NewMapping : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/remotepathmapping";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0)]
        public string HostName { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public string LocalPath { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public string RemotePath { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            string msg = string.Format("Host: {0}; LocalPath: '{1}'; RemotePath: '{2}'", this.HostName, this.LocalPath, this.RemotePath);
            if (base.ShouldProcess(msg, "New"))
            {
                RemotePathMapping rpm = base.SendSonarrPost<RemotePathMapping>(EP, this.GetBodyParameters());
                base.SendToPipeline(rpm);
            }
        }

        #endregion

        #region BACKEND METHODS
        private SonarrBodyParameters GetBodyParameters() => new SonarrBodyParameters(3)
        {
            { "host", this.HostName },
            { "localPath", this.LocalPath },
            { "remotePath", this.RemotePath }
        };

        #endregion
    }
}