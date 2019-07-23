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
    [Cmdlet(VerbsLifecycle.Invoke, "SeriesRefresh", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [OutputType(typeof(CommandOutput))]
    [Alias("Start-SeriesRefresh")]
    [CmdletBinding(PositionalBinding = false)]
    public class InvokeSeriesRefresh : BasePostCommandCmdlet
    {
        #region FIELDS/CONSTANTS
        protected override string Command => "RefreshSeries";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0, ValueFromPipelineByPropertyName = true)]
        public long SeriesId { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            string msg = "Refresh all series";
            if (this.MyInvocation.BoundParameters.ContainsKey("SeriesId"))
            {
                parameters.Add("seriesId", this.SeriesId);
                msg = string.Format("Refresh series id {0}", this.SeriesId);
            }

            if (this.Force.ToBool() || base.ShouldProcess(msg, "Invoke"))
            {
                base.ProcessRecord();
            }
        }

        #endregion
    }
}