using MG.Posh.Extensions.Bound;
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
            if (this.ContainsParameter(x => x.JobId))
            {
                for (int i = 0; i < this.JobId.Length; i++)
                {
                    string ep = string.Format(EP_ID, this.JobId[i]);
                    CommandResult result = base.SendSonarrGet<CommandResult>(ep);
                    if (result != null)
                    {
                        base.WriteObject(result);
                        this.AddToHistory(result);
                    }
                }
            }
            else
            {
                List<CommandResult> list = base.SendSonarrListGet<CommandResult>(EP);
                if (list != null && list.Count > 0)
                {
                    base.WriteObject(list, true);
                    this.AddToHistory(list);
                }
            }
        }

        #endregion

        private void AddToHistory(CommandResult result) => History.Jobs.AddResult(result);
        private void AddToHistory(IEnumerable<CommandResult> results) => History.Jobs.AddResults(results);
    }
}