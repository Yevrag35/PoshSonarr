using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

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
            List<Release> releases = base.SendSonarrListGet<Release>(ep);
            if (releases != null && releases.Count > 0)
                base.WriteObject(this.Filter(releases), true);
        }

        #endregion

        #region METHODS
        private List<Release> Filter(List<Release> releases)
        {
            if (_exclude)
                return releases.FindAll(x => !x.IsRejected);

            else
                return releases;
        }

        #endregion
    }
}