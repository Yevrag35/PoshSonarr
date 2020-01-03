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

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByTagLabel")]
        [SupportsWildcards]
        public string[] Label { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByTagId", ValueFromPipelineByPropertyName = true)]
        [Alias("Tags")]
        public int[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if ( ! base.HasParameterSpecified(this, x => x.Id) && this.TryGetAllTags(out List<Tag> allTags))
            {
                base.SendToPipeline(base.FilterByStringParameter(allTags, t => t.Label, this, cmd => cmd.Label));
            }
            else if (base.HasParameterSpecified(this, x => x.Id))
            {
                base.SendToPipeline(this.GetTagById(this.Id));
            }
        }

        #endregion

        #region BACKEND METHODS
        private bool TryGetAllTags(out List<Tag> outTags)
        {
            bool result = false;
            outTags = base.SendSonarrListGet<Tag>(EP);
            return outTags != null && outTags.Count > 0;
        }

        private IEnumerable<Tag> GetTagById(int[] ids)
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