using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Restart, "Instance", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [Alias("Restart-")]
    public class RestartInstance : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/system/restart";
        private bool _force;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false)]
        public SwitchParameter Force
        {
            get => _force;
            set => _force = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (_force || base.ShouldProcess("Sonarr service", "Restart"))
            {
                base.SendSonarrPostNoData(EP);
            }
        }

        #endregion
    }
}