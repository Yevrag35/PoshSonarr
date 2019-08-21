using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Remove, "Restriction", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [CmdletBinding(PositionalBinding = false)]
    public class RemoveRestriction : BaseSonarrCmdlet
    {
        private bool _force;

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public int RestrictionId { get; set; }

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
            if (_force || base.ShouldProcess(string.Format("Restriction Id: {0}", this.RestrictionId), "Remove"))
            {
                base.TryDeleteSonarrResult(string.Format(GetRestriction.EP_ID, this.RestrictionId));
            }
        }

        #endregion
    }
}