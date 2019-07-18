using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.PowerShell.Commands;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "History", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    public class GetHistory : BaseSonarrCmdlet
    {
        [Parameter(Mandatory = false)]
        [ValidateSet("Date", "Series-Title")]
        public string SortKey = "Date";

        [Parameter(Mandatory = false)]
        public int Page = 1;

        [Parameter(Mandatory = false)]
        public int PageSize = 10;

        [Parameter(Mandatory = false)]
        [ValidateSet("Ascending", "Descending")]
        public string SortDirection = "Ascending";

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public long EpisodeId { get; set; }

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            var list = new List<string>();
            if (this.MyInvocation.BoundParameters.ContainsKey("SortKey"))
            {
                if (this.SortKey == "Date")
                    list.Add("sortKey=date");

                else
                    list.Add("sortKey=series.title");
            }

            if (this.MyInvocation.BoundParameters.ContainsKey("Page"))
                list.Add(string.Format("page={0}", this.Page));

            if (this.MyInvocation.BoundParameters.ContainsKey("PageSize"))
                list.Add(string.Format("pageSize={0}", this.PageSize));

            if (this.MyInvocation.BoundParameters.ContainsKey("SortDirection"))
            {
                if (this.SortDirection == "Ascending")
                    list.Add("sortDir=asc");

                else
                    list.Add("sortDir=desc");
            }

            if (this.MyInvocation.BoundParameters.ContainsKey("EpisodeId"))
            {
                list.Add(string.Format("episodeId={0}", this.EpisodeId));
            }

            string full = "/history";
            if (list.Count > 0)
            {
                full = full + "?" + string.Join("&", list);
            }

            string jsonRes = base.TryGetSonarrResult(full);
            if (!string.IsNullOrEmpty(jsonRes))
            {
                base.WriteObject(JsonObject.ConvertFromJson(jsonRes, false, out ErrorRecord er));
            }
        }
    }
}
