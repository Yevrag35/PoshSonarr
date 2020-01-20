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
    public class GetTag : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        protected private const string EP = "/tag";
        protected private const string EP_WITH_ID = EP + "/{0}";

        private HashSet<int> _ids;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByInputTagObject")]
        public IHasTagSet InputObject { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByTagLabel")]
        [SupportsWildcards]
        public string[] Label { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByTagId", ValueFromPipelineByPropertyName = true)]
        [Alias("Tags")]
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
            if (!base.HasParameterSpecified(this, x => x.Id) && this.TryGetAllTags(out List<Tag> allTags))
            {
                allTags.Sort();
                base.SendToPipeline(base.FilterByStringParameter(allTags, t => t.Label, this, cmd => cmd.Label));
            }
            else if (_ids.Count > 0)
            {
                base.SendToPipeline(this.GetTagById(_ids));
            }
        }

        #endregion

        #region BACKEND METHODS
        private bool TryGetAllTags(out List<Tag> outTags)
        {
            outTags = base.SendSonarrListGet<Tag>(EP);
            return outTags != null && outTags.Count > 0;
        }

        private IEnumerable<Tag> GetTagById(IEnumerable<int> ids)
        {
            foreach (int id in ids)
            {
                string endpoint = string.Format(EP_WITH_ID, id);
                Tag oneTag = base.SendSonarrGet<Tag>(endpoint);
                if (oneTag != null)
                    yield return oneTag;
            }
        }

        #endregion
    }
}