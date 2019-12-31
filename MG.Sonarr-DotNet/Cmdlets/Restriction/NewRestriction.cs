using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "Restriction", ConfirmImpact = ConfirmImpact.None, SupportsShouldProcess = true)]
    [OutputType(typeof(Restriction))]
    public class NewRestriction : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        internal const string SHOULD_MSG = "Restriction: Ignored - {{ {0} }}, Required - {{ {1} }}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0)]
        [Alias("Ignored")]
        public string[] IgnoredTerms { get; set; }

        [Parameter(Mandatory = false, Position = 1)]
        [Alias("Required")]
        public string[] RequiredTerms { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            Restriction newRestrict = this.ParametersToRestriction(this.MyInvocation.BoundParameters);
            if (base.ShouldProcess(string.Format(SHOULD_MSG, newRestrict.Ignored.ToJson(), newRestrict.Required.ToJson()), "New"))
            {
                Restriction restriction = base.SendSonarrPost<Restriction>(GetRestriction.EP, newRestrict);
                base.SendToPipeline(restriction);
            }
        }

        #endregion

        #region BACKEND METHODS
        private Restriction ParametersToRestriction(Dictionary<string, object> parameters) => new Restriction(parameters);

        #endregion
    }
}