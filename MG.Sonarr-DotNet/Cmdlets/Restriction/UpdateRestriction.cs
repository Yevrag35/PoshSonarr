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
    [Cmdlet(VerbsData.Update, "Restriction", ConfirmImpact = ConfirmImpact.None, SupportsShouldProcess = true)]
    [OutputType(typeof(Restriction))]
    [CmdletBinding(PositionalBinding = false)]
    public class UpdateRestriction : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private bool _force;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public Restriction InputObject { get; set; }

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
            if (_force || base.ShouldProcess(string.Format("Restriction: {0}", this.InputObject.Id), "Update"))
            {
                Restriction newRestriction = this.Update(this.InputObject);
                base.SendToPipeline(newRestriction);
            }
        }

        #endregion

        #region BACKEND METHODS
        private Restriction Update(Restriction restriction)
        {
            string url = string.Format(GetRestriction.EP_ID, restriction.Id);
            return base.SendSonarrPut<Restriction>(url, restriction);
        }

        #endregion
    }
}