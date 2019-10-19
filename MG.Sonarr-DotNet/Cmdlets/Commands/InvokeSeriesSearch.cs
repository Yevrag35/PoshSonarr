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
#if NETFRAMEWORK
    /// <summary>
    /// <para type="synopsis">Executes a series search.</para>
    /// <para type="description">Instructs Sonarr to perform a series search for the specified seriesID.</para>
    /// </summary>
#endif
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
        /// <summary>
        /// <para type="description">The series ID to perform the series search on.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
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