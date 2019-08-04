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
    [Cmdlet(VerbsCommon.Get, "Restriction", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByIgnoredTags")]
    [OutputType(typeof(Restriction))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetRestriction : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        internal const string EP = "/restriction";
        internal const string EP_ID = EP + "/{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ParameterSetName = "ByRestrictionId")]
        public int[] Id { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByIgnoredTags")]
        [SupportsWildcards]
        public string[] IgnoredTags { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.ParameterSetName != "ByRestrictionId")
            {
                List<Restriction> restrictions = this.GetAllRestrictions();
                if (!this.MyInvocation.BoundParameters.ContainsKey("IgnoredTags"))
                    base.WriteObject(restrictions, true);

                else
                    base.WriteObject(this.FilterByIgnored(this.IgnoredTags, restrictions), true);
            }
            else
            {
                for (int i = 0; i < this.Id.Length; i++)
                {
                    base.WriteObject(this.GetRestrictionById(this.Id[i]));
                }
            }
        }

        #endregion

        #region BACKEND METHODS
        private List<Restriction> FilterByIgnored(string[] ignored, List<Restriction> restrictions)
        {
            var wcps = new List<WildcardPattern>(ignored.Length);
            for (int i = 0; i < ignored.Length; i++)
            {
                wcps.Add(new WildcardPattern(ignored[i], WildcardOptions.IgnoreCase));
            }

            return restrictions.FindAll(r => wcps.Exists(w => r.Ignored.Any(inner => w.IsMatch(inner))));
        }

        private List<Restriction> GetAllRestrictions()
        {
            string jsonRes = base.TryGetSonarrResult(EP);
            return !string.IsNullOrEmpty(jsonRes)
                ? SonarrHttp.ConvertToSonarrResults<Restriction>(jsonRes)
                : null;
        }

        private Restriction GetRestrictionById(int id)
        {
            string jsonRes = base.TryGetSonarrResult(string.Format(EP_ID, id));
            return SonarrHttp.ConvertToSonarrResult<Restriction>(jsonRes);
        }

        #endregion
    }
}