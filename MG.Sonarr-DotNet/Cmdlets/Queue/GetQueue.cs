using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Queue", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(QueueItem))]
    public class GetQueue : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/queue";
        private const string EP_ID = EP + "/{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0)]
        public long[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.MyInvocation.BoundParameters.ContainsKey("Id"))
            {
                for (int i = 0; i < this.Id.Length; i++)
                {
                    string ep = string.Format(EP_ID, this.Id[i]);
                    string jsonRes = base.TryGetSonarrResult(ep);
                    if (!string.IsNullOrEmpty(jsonRes))
                    {
                        var oneRes = SonarrHttp.ConvertToSonarrResult<QueueItem>(jsonRes);
                        base.WriteObject(oneRes);
                    }
                }
            }
            else
            {
                string jsonRes = base.TryGetSonarrResult(EP);
                if (!string.IsNullOrEmpty(jsonRes))
                {
                    var resses = SonarrHttp.ConvertToSonarrResults<QueueItem>(jsonRes, out bool iso);
                    base.WriteObject(resses, true);
                }
            }
        }

        #endregion
    }
}