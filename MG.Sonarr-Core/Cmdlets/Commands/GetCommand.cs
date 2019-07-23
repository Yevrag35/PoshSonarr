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
                        var output = SonarrHttpClient.ConvertToSonarrResult<CommandResult>(jsonRes);
                        base.WriteObject(output);
                    }
                }
            }
            else
            {
                string jsonRes = base.TryGetSonarrResult(EP);
                if (!string.IsNullOrEmpty(jsonRes))
                {
                    var jobs = SonarrHttpClient.ConvertToSonarrResults<CommandResult>(jsonRes, out bool iso);
                    base.WriteObject(jobs, true);
                }
            }
        }

        #endregion
    }
}