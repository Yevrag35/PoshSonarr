using MG.Sonarr.Functionality;
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
    [Cmdlet(VerbsCommon.Get, "Tag", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByTagLabel")]
    [OutputType(typeof(Tag))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetTag : TagCmdlet
    {
        #region FIELDS/CONSTANTS
        private HashSet<int> _ids;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByInputTagObject")]
        public ISupportsTagUpdate InputObject { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByTagLabel")]
        [SupportsWildcards]
        public string[] Label { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByTagId")]
        public int[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _ids = new HashSet<int>();
        }

        protected override void ProcessRecord()
        {
            if (base.HasParameterSpecified(this, x => x.InputObject) && this.InputObject.Tags != null && this.InputObject.Tags.Count > 0)
            {
                _ids.UnionWith(this.InputObject.Tags);
            }
            else if (base.HasParameterSpecified(this, x => x.Id))
            {
                _ids.UnionWith(this.Id);
            }
        }

        protected override void EndProcessing()
        {
            if (!base.HasParameterSpecified(this, x => x.Id) && base.TryGetAllTags(out TagCollection allTags))
            {
                allTags.Sort();
                base.SendToPipeline(base.FilterByStringParameter(allTags, t => t.Label, this, cmd => cmd.Label));
            }
            else if (_ids.Count > 0)
            {
                base.SendToPipeline(base.GetTagById(_ids));
            }
        }

        #endregion
    }
}