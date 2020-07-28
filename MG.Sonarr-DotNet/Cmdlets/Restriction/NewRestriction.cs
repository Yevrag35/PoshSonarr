using MG.Dynamic;
using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality.Tags;
using MG.Sonarr.Functionality.Extensions;
using MG.Sonarr.Results;
using System;
using System.Management.Automation;
using System.Linq;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "Restriction", ConfirmImpact = ConfirmImpact.None, SupportsShouldProcess = true)]
    [OutputType(typeof(Restriction))]
    [CmdletBinding(PositionalBinding = false)]
    public class NewRestriction : BaseSonarrCmdlet, IDynamicParameters
    {
        #region FIELDS/CONSTANTS
        internal const string SHOULD_MSG = "Restriction: Ignored - {{ {0} }}, Required - {{ {1} }}";
        private DynamicLibrary _lib;
        private const string PARAM = "Tag";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0)]
        [Alias("Ignored")]
        public string[] IgnoredTerms { get; set; }

        [Parameter(Mandatory = false, Position = 1)]
        [Alias("Required")]
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
            var newRestrict = new Restriction();
            if (_lib.ParameterHasValue(PARAM))
            {
                newRestrict.Tags.UnionWith(_lib.GetUnderlyingValues<Tag>(PARAM).Select(x => x.Id));
            }

            if (this.ContainsParameter(x => x.IgnoredTerms))
                newRestrict.Ignored.UnionWith(this.IgnoredTerms);

            if (this.ContainsParameter(x => x.RequiredTerms))
                newRestrict.Required.UnionWith(this.RequiredTerms);

            if (base.ShouldProcess(string.Format(SHOULD_MSG, newRestrict.Ignored.ToJson(), newRestrict.Required.ToJson()), "New"))
            {
                Restriction restriction = base.SendSonarrPost<Restriction>(GetRestriction.EP, newRestrict);
                base.SendToPipeline(restriction);
            }
        }

        #endregion
    }
}