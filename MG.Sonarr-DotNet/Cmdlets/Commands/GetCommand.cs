using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets.Commands
{
#if (NETCOREAPP == false)
    /// <summary>
    ///     <para type="synopsis">Retrieves the status of a given command issued to Sonarr.</para>
    ///     <para type="description">After issuing a command to Sonarr, this command can retrieve additional details about 
    ///         how it ran.
    ///     </para>
    /// </summary>
#endif
    [Cmdlet(VerbsCommon.Get, "Command", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(CommandResult))]
    [Alias("Get-Job")]
    [CmdletBinding(PositionalBinding = false)]
    public class GetCommand : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/command";
        private const string EP_ID = EP + "/{0}";

        #endregion

        #region PARAMETERS
        /// <summary>
        /// <para type="description">The job id(s) of the command(s) to retrieves their statuses.</para>
        /// </summary>
        [Parameter(Mandatory = false, Position = 0, ValueFromPipelineByPropertyName = true)]
        public long[] JobId { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.MyInvocation.BoundParameters.ContainsKey("JobId"))
            {
                for (int i = 0; i < this.JobId.Length; i++)
                {
                    string ep = string.Format(EP_ID, this.JobId[i]);
                    string jsonRes = base.TryGetSonarrResult(ep);
                    if (!string.IsNullOrEmpty(jsonRes))
                    {
                        CommandResult output = SonarrHttp.ConvertToSonarrResult<CommandResult>(jsonRes);
                        base.WriteObject(output);
                    }
                }
            }
            else
            {
                string jsonRes = base.TryGetSonarrResult(EP);
                if (!string.IsNullOrEmpty(jsonRes))
                {
                    List<CommandResult> jobs = SonarrHttp.ConvertToSonarrResults<CommandResult>(jsonRes, out bool iso);
                    base.WriteObject(jobs, true);
                }
            }
        }

        #endregion
    }
}