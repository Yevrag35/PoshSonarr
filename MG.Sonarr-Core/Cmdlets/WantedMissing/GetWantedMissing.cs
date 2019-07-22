using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "WantedMissing", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    public class GetWantedMissing : LazySonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        protected override string Endpoint => "/wanted/missing";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false)]
        [ValidateSet("AirDateUtc", "Series-Title")]
        public string SortKey = "AirDateUtc";

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.MyInvocation.BoundParameters.ContainsKey("SortKey"))
            {
                if (this.SortKey.Equals("AirDateUtc", StringComparison.CurrentCultureIgnoreCase))
                    _list.Add("sortKey=airDateUtc");

                else
                    _list.Add("sortKey=series.title");
            }

            base.ProcessRecord();
        }

        #endregion
    }
}