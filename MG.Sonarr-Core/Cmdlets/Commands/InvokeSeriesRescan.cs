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
    [Cmdlet(VerbsLifecycle.Invoke, "SeriesRescan", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [OutputType(typeof(CommandOutput))]
    [Alias("Start-SeriesRescan")]
    [CmdletBinding(PositionalBinding = false)]
    public class InvokeSeriesRescan : BasePostCommandCmdlet
    {
        #region FIELDS/CONSTANTS
        protected override string Command => "RescanSeries";

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
            var newDict = new Dictionary<string, object>(parameters)
            {
                { "seriesId", this.SeriesId }
            };

            string msg = "Rescan all series";
            if (this.MyInvocation.BoundParameters.ContainsKey("SeriesId"))
            {
                msg = string.Format("Rescan series id {0}", this.SeriesId);
            }

            if (this.Force.ToBool() || base.ShouldProcess(msg, "Invoke"))
            {
                base.ProcessRequest(newDict);
            }
        }

        #endregion
    }
}