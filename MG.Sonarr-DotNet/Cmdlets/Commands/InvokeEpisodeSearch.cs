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
    /// <summary>
    /// <para type="synopsis">Instructs Sonarr to perform an episode search.</para>
    /// <para type="description">Tells SOnarr to perform an episode search for the given episodes.</para>
    /// </summary>
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
        /// <summary>
        /// <para type="description">The episode id(s) retrieved from 'Get-SonarrEpisode'.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public long[] EpisodeId { get; set; }

        /// <summary>
        /// <para type="description">Specify to bypass confirmation prompts.</para>
        /// </summary>
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