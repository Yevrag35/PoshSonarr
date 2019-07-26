using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "History", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    public class GetHistory : LazySonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        protected override string Endpoint => "/history";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false)]
        [ValidateSet("Date", "Series-Title")]
        public string SortKey = "Date";

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public long EpisodeId { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.MyInvocation.BoundParameters.ContainsKey("SortKey"))
            {
                if (this.SortKey.Equals("Date", StringComparison.CurrentCultureIgnoreCase))
                    _list.Add("sortKey=date");

                else
                    _list.Add("sortKey=series.title");
            }

            if (this.MyInvocation.BoundParameters.ContainsKey("EpisodeId"))
            {
                _list.Add(string.Format("episodeId={0}", this.EpisodeId));
            }
            base.ProcessRecord();
        }

        #endregion
    }
}