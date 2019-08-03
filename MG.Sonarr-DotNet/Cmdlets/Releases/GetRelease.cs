using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Release", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(Release))]
    public class GetRelease : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/release?episodeId={0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public long EpisodeId { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            string ep = string.Format(EP, this.EpisodeId);
            string jsonRes = base.TryGetSonarrResult(ep);
            if (!string.IsNullOrEmpty(jsonRes))
            {
                var results = SonarrHttp.ConvertToSonarrResults<Release>(jsonRes, out bool iso);
                base.WriteObject(results, true);
            }
        }

        #endregion

        #region METHODS


        #endregion
    }
}