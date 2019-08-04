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
    [Cmdlet(VerbsData.Update, "Restriction", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
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
            if (_force || base.ShouldProcess(string.Format("Restriction: {0}", this.InputObject.RestrictionId), "Update"))
            {
                Restriction newRestriction = this.Update(this.InputObject);
                base.WriteObject(newRestriction);
            }
        }

        #endregion

        #region BACKEND METHODS
        private Restriction Update(Restriction restriction)
        {
            string url = string.Format(GetRestriction.EP_ID, restriction.RestrictionId);
            string jsonRes = base.TryPutSonarrResult(url, restriction.ToJson());
            return SonarrHttp.ConvertToSonarrResult<Restriction>(jsonRes);
        }

        #endregion
    }
}