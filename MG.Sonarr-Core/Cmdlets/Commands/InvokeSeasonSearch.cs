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
    [Cmdlet(VerbsLifecycle.Invoke, "SeasonSearch", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(CommandOutput))]
    [Alias("Start-SeasonSearch")]
    public class InvokeSeasonSearch : BasePostCommandCmdlet
    {
        #region FIELDS/CONSTANTS
        protected override string Command => "SeasonSearch";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public long SeriesId { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int SeasonNumber { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            var newDict = new Dictionary<string, object>(parameters)
            {
                { "seriesId", this.SeriesId },
                { "seasonNumber", this.SeasonNumber }
            };

            string msg = string.Format("Series Id {0}; Season {1}", this.SeriesId, this.SeasonNumber);
            if (this.Force.ToBool() || base.ShouldProcess(msg, "Invoke Season Search"))
            {
                base.ProcessRequest(newDict);
            }
        }

        #endregion

        #region METHODS


        #endregion
    }
}