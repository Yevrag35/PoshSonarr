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
#if NETFRAMEWORK
    /// <summary>
    /// <para type="synopsis">Executes a series rescan.</para>
    /// <para type="description">Instructs Sonarr to perform a series rescan on all series or a specified series by its ID.</para>
    /// </summary>
#endif
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
        /// <summary>
        /// <para type="description">The optional series ID to perform the rescan on.</para>
        /// </summary>
        [Parameter(Mandatory = false, Position = 0, ValueFromPipelineByPropertyName = true)]
        public long SeriesId { get; set; }

        /// <summary>
        /// <para type="description">Specifies to bypass the confirmation prompt.</para>
        /// </summary>
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