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
        protected private const string EP_ID = EP + "/{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByTagLabel")]
        [SupportsWildcards]
        public string[] Label { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByTagId")]
        public int[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.ParameterSetName == "ByTagLabel")
            {
                string allRes = base.TryGetSonarrResult(EP);
                List<Tag> tags = null;
                if (!string.IsNullOrEmpty(allRes))
                {
                    tags = SonarrHttpClient.ConvertToSonarrResults<Tag>(allRes, out bool iso);
                }
                if (tags.Count > 0)
                {
                    if (this.MyInvocation.BoundParameters.ContainsKey("Label"))
                    {
                        for (int i = 0; i < this.Label.Length; i++)
                        {
                            var wcp = new WildcardPattern(this.Label[i], WildcardOptions.IgnoreCase);
                            for (int t = 0; t < tags.Count; t++)
                            {
                                Tag oneTag = tags[t];
                                if (wcp.IsMatch(oneTag.Label))
                                    base.WriteObject(oneTag);
                            }
                        }
                    }
                    else
                        base.WriteObject(tags, true);
                }

            }
            else
            {
                for (int i = 0; i < this.Id.Length; i++)
                {
                    string ep = string.Format(EP_ID, this.Id[i]);
                    string jsonRes = base.TryGetSonarrResult(ep);
                    if (!string.IsNullOrEmpty(jsonRes))
                    {
                        base.WriteObject(SonarrHttpClient.ConvertToSonarrResult<Tag>(jsonRes));
                    }
                }
            }
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}