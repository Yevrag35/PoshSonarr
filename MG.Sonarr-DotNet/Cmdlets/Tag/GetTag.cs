using MG.Posh.Extensions.Bound;
using MG.Sonarr.Extensions;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Tag", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByTagLabel")]
    [OutputType(typeof(Tag))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetTag : TagCmdlet
    {
        #region FIELDS/CONSTANTS
        private HashSet<int> _ids;
        private HashSet<string> _labels;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByInputTagObject")]
        public ISupportsTagUpdate InputObject { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByTagLabel")]
        [SupportsWildcards]
        [Alias("l")]
        //public string[] Label { get; set; }
        public object[] Label { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByTagId")]
        public int[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _ids = new HashSet<int>();
            _labels = new HashSet<string>();
            this.ProcessStringOrIdParameter(this.Label, _labels, _ids);
        }

        protected override void ProcessRecord()
        {
            if (this.ContainsParameter(x => x.InputObject) && this.InputObject.Tags != null && this.InputObject.Tags.Count > 0)
            {
                _ids.UnionWith(this.InputObject.Tags);
            }
            else if (this.ContainsParameter(x => x.Id))
            {
                _ids.UnionWith(this.Id);
            }
        }

        protected override void EndProcessing()
        {
            if (_ids.Count <= 0 && base.TryGetAllTags(out ITagCollection allTags))
            {
                allTags.Sort();
                base.SendToPipeline(base.FilterByStringParameter(allTags, t => t.Label, this, cmd => cmd._labels));
            }
            else if (_ids.Count > 0)
            {
                base.SendToPipeline(base.GetTagById(_ids));
            }
        }

        #endregion
    }
}