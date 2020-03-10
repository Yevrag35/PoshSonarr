using MG.Posh.Extensions.Bound;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets.Episodes
{
    [Cmdlet(VerbsCommon.Remove, "EpisodeFile", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true, DefaultParameterSetName = "ByEpisodeFile")]
    [CmdletBinding(PositionalBinding = false)]
    public class RemoveEpisodeFile : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/episodefile/{0}";
        private bool _force;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByEpisodeFile")]
        public EpisodeFile EpisodeFile { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByEpisodeFileId")]
        public int EpisodeFileId { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force
        {
            get => _force;
            set => _force = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.ContainsParameter(x => x.EpisodeFile))
            {
                this.EpisodeFileId = this.EpisodeFile.Id;
            }

            if (this.EpisodeFileId.Equals(0))
            {
                throw new ArgumentNullException("EpisodeFileId");
            }

            string ep = string.Format(EP, this.EpisodeFileId);
            if (_force || base.ShouldProcess(ep, "Delete"))
            {
                base.SendSonarrDelete(ep);
            }
        }

        #endregion
    }
}