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
    [Cmdlet(VerbsCommon.Remove, "EpisodeFile", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true, DefaultParameterSetName = "ByEpisodeResult")]
    [CmdletBinding(PositionalBinding = false)]
    public class RemoveEpisodeFile : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/episodefile/{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByEpisodeResult")]
        public EpisodeResult Episode { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0, ParameterSetName = "ByEpisodeFile")]
        public long EpisodeFileId { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.MyInvocation.BoundParameters.ContainsKey("Episode"))
                this.EpisodeFileId = this.Episode.EpisodeFile.EpisodeFileId;

            if (this.EpisodeFileId.Equals(0))
                throw new ArgumentNullException("EpisodeFileId");

            string ep = string.Format(EP, this.EpisodeFileId);
            if (this.Force.ToBool() || base.ShouldProcess(ep, "Delete"))
            {
                base.TryDeleteSonarrResult(ep);
            }
        }

        #endregion
    }
}