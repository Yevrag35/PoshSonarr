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
    [Cmdlet(VerbsCommon.Search, "Release", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(Release))]
    [Alias("Get-Release")]
    public class SearchRelease : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/release?episodeId={0}";
        private bool _exclude;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, DontShow = true)]
        public long EpisodeId { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter ExcludeRejected
        {
            get => _exclude;
            set => _exclude = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            string ep = string.Format(EP, this.EpisodeId);
            string jsonRes = base.TryGetSonarrResult(ep);
            if (!string.IsNullOrEmpty(jsonRes))
            {
                List<Release> results = SonarrHttp.ConvertToSonarrResults<Release>(jsonRes, out bool iso);
                if (_exclude)
                    base.WriteObject(results.FindAll(x => !x.IsRejected), true);

                else
                    base.WriteObject(results, true);
            }
        }

        #endregion

        #region METHODS


        #endregion
    }
}