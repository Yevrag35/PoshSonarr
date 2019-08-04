using MG.Sonarr.Results;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "Restriction", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(Restriction))]
    public class NewRestriction : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string SHOULD_MSG = "Restriction: Ignored - {{ {0} }}, Required - {{ {1} }}";

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
                string jsonRes = base.TryPostSonarrResult(GetRestriction.EP, newRestrict.ToJson());
                if (!string.IsNullOrEmpty(jsonRes))
                {
                    Restriction restRes = SonarrHttp.ConvertToSonarrResult<Restriction>(jsonRes);
                    base.WriteObject(restRes);
                }
            }
        }

        #endregion

        #region BACKEND METHODS
        private Restriction ParametersToRestriction(Dictionary<string, object> parameters) => new Restriction(parameters);

        #endregion
    }
}