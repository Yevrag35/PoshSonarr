using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality.Extensions;
using MG.Sonarr.Results;
using System;
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
            var newRestrict = new Restriction();
            if (this.ContainsParameter(x => x.IgnoredTerms))
                newRestrict.Ignored.AddRange(this.IgnoredTerms);

            if (this.ContainsParameter(x => x.RequiredTerms))
                newRestrict.Required.AddRange(this.RequiredTerms);

            if (base.ShouldProcess(string.Format(SHOULD_MSG, newRestrict.Ignored.ToJson(), newRestrict.Required.ToJson()), "New"))
            {
                Restriction restriction = base.SendSonarrPost<Restriction>(GetRestriction.EP, newRestrict);
                base.SendToPipeline(restriction);
            }
        }

        #endregion
    }
}