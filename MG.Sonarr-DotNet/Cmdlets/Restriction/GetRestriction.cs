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
            if ( ! base.HasParameterSpecified(this, x => x.Id))
            {
                List<Restriction> restrictions = this.GetAllRestrictions();
                base.SendToPipeline(base.FilterByStringParameter(restrictions, r => r.Ignored, this, cmd => cmd.IgnoredTags));
            }
            else
            {
                for (int i = 0; i < this.Id.Length; i++)
                {
                    base.SendToPipeline(this.GetRestrictionById(this.Id[i]));
                }
            }
        }

        #endregion

        #region BACKEND METHODS
        private List<Restriction> FilterByIgnored(string[] ignored, List<Restriction> restrictions)
        {
            IEnumerable<WildcardPattern> patterns = ignored
                .Select(x => new WildcardPattern(x, WildcardOptions.IgnoreCase));

            return restrictions.FindAll(r => patterns.Any(w => r.Ignored.Any(inner => w.IsMatch(inner))));
        }

        private List<Restriction> GetAllRestrictions() => base.SendSonarrListGet<Restriction>(EP);

        private Restriction GetRestrictionById(int id)
        {
            string endpoint = string.Format(EP_ID, id);
            return base.SendSonarrGet<Restriction>(endpoint);
        }

        #endregion
    }
}