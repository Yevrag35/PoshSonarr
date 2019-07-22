using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.PowerShell.Commands;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    public abstract class LazySonarrCmdlet : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        protected private string _ep;
        protected private List<string> _list;
        protected abstract string Endpoint { get; }

        #endregion

        [Parameter(Mandatory = false)]
        public int Page = 1;

        [Parameter(Mandatory = false)]
        public int PageSize = 10;

        [Parameter(Mandatory = false)]
        [ValidateSet("Ascending", "Descending")]
        public string SortDirection = "Ascending";

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _list = new List<string>();
        }

        protected override void ProcessRecord()
        {
            if (this.MyInvocation.BoundParameters.ContainsKey("Page"))
                _list.Add(string.Format("page={0}", this.Page));

            if (this.MyInvocation.BoundParameters.ContainsKey("PageSize"))
                _list.Add(string.Format("pageSize={0}", this.PageSize));

            if (this.MyInvocation.BoundParameters.ContainsKey("SortDirection"))
            {
                if (this.SortDirection == "Ascending")
                    _list.Add("sortDir=asc");

                else
                    _list.Add("sortDir=desc");
            }

            _ep = this.Endpoint;
            if (_list.Count > 0)
            {
                _ep = _ep + "?" + string.Join("&", _list);
            }

            string jsonRes = base.TryGetSonarrResult(_ep);
            if (!string.IsNullOrEmpty(jsonRes))
            {
                base.WriteObject(JsonObject.ConvertFromJson(jsonRes, false, out ErrorRecord er));
            }
        }
    }
}
