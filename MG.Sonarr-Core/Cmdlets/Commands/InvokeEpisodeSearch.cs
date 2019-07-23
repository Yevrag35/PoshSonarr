using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets.Commands
{
    [Cmdlet(VerbsLifecycle.Invoke, "EpisodeSearch", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(CommandOutput))]
    [Alias("Start-EpisodeSearch")]
    public class InvokeEpisodeSearch : BasePostCommandCmdlet
    {
        #region FIELDS/CONSTANTS
        protected override string Command => "EpisodeSearch";
        private List<long> _ids;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public long[] EpisodeId { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _ids = new List<long>(10);
        }

        protected override void ProcessRecord()
        {
            _ids.AddRange(this.EpisodeId);
        }

        protected override void EndProcessing()
        {
            if (this.Force.ToBool() || base.ShouldProcess(string.Format("Search episodes {0}", string.Join(", ", _ids)), "Invoke"))
            {
                parameters.Add("episodeIds", _ids);
                base.ProcessRequest(parameters);
            }
        }

        #endregion
    }
}