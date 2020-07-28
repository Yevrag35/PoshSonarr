using MG.Dynamic;
using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality.Strings;
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
    [Cmdlet(VerbsCommon.Get, "Restriction", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "None")]
    [OutputType(typeof(Restriction))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetRestriction : BaseSonarrCmdlet, IDynamicParameters
    {
        #region FIELDS/CONSTANTS
        private const string PARAM = "Tag";
        private DynamicLibrary _lib;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ParameterSetName = "ById")]
        public int[] Id { get; set; }

        [Parameter(Mandatory = false)]
        [Alias("Ignored")]
        [SupportsWildcards]
        public string[] IgnoredTerms { get; set; }

        [Parameter(Mandatory = false)]
        [Alias("Required")]
        [SupportsWildcards]
        public string[] RequiredTerms { get; set; }

        #endregion

        public object GetDynamicParameters()
        {
            _lib = new DynamicLibrary();
            if (Context.IsConnected)
            {
                var idp = new DynamicParameter<Tag>(PARAM, true, Context.TagManager.AllTags, x => x.Label)
                {
                    Mandatory = false
                };
                _lib.Add(idp);
            }
            return _lib;
        }

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if ( ! this.ContainsParameter(x => x.Id))
            {
                IEnumerable<Restriction> restrictions = this.GetAllRestrictions();
                restrictions = base.FilterByStringParameter(restrictions, r => r.Ignored, this, cmd => cmd.IgnoredTerms);
                restrictions = base.FilterByStringParameter(restrictions, r => r.Required, this, cmd => cmd.RequiredTerms);
                if (_lib.ParameterHasValue(PARAM))
                {
                    IEnumerable<Tag> tags = _lib.GetUnderlyingValues<Tag>(PARAM);
                    restrictions = restrictions
                        .Where(x => x.Tags.Overlaps(tags.Select(t => t.Id)));
                }
                base.SendToPipeline(restrictions);
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

        private List<Restriction> GetAllRestrictions() => base.SendSonarrListGet<Restriction>(ApiEndpoints.Restriction);

        private Restriction GetRestrictionById(int id)
        {
            string endpoint = string.Format(ApiEndpoints.Restriction_ById, id);
            return base.SendSonarrGet<Restriction>(endpoint);
        }

        #endregion
    }
}