using MG.Api.Rest.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

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

        #region BACKEND METHODS


        #endregion
    }
}