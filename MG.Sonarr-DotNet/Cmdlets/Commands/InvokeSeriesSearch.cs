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
    [Cmdlet(VerbsLifecycle.Invoke, "SeriesSearch", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(CommandOutput))]
    [Alias("Start-SeriesSearch")]
    public class InvokeSeriesSearch : BasePostCommandCmdlet
    {
        #region FIELDS/CONSTANTS
        protected override string Command => "SeriesSearch";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
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
            string msg = string.Format("Series Id {0}", this.SeriesId);
            if (this.Force.ToBool() || base.ShouldProcess(msg, "Invoke Series Search"))
            {
                base.ProcessRequest(newDict);
            }
        }

        #endregion

        #region METHODS


        #endregion
    }
}