﻿using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Results;
using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
#if NETFRAMEWORK
    /// <summary>
    /// <para type="synopsis">Execute a series refresh.</para>
    /// <para type="description">Instructs Sonarr to perform a series refresh for either all series or a specified series by ID.</para>
    /// </summary>
#endif
    [Cmdlet(VerbsLifecycle.Invoke, "SeriesRefresh", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [OutputType(typeof(CommandOutput))]
    [Alias("Start-SeriesRefresh")]
    [CmdletBinding(PositionalBinding = false)]
    public class InvokeSeriesRefresh : BasePostCommandCmdlet
    {
        #region FIELDS/CONSTANTS
        protected sealed override string Command => "RefreshSeries";

        #endregion

        #region PARAMETERS
        /// <summary>
        /// <para type="description">THe optional series ID to perform the refresh on.</para>
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
            var newDict = new SonarrBodyParameters(parameters)
            {
                { "seriesId", this.SeriesId }
            };

            string msg = "Refresh all series";

            if (this.ContainsParameter(x => x.SeriesId))
            {
                msg = string.Format("Refresh series id {0}", this.SeriesId);
            }
            if (this.Force.ToBool() || base.ShouldProcess(msg, "Invoke"))
            {
                base.ProcessRequest(newDict);
            }
        }

        #endregion
    }
}