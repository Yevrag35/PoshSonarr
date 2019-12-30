using MG.Sonarr.Functionality;
using Microsoft.PowerShell.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    public abstract class LazySonarrCmdlet : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private string _ep;
        protected private List<string> _list;

        private const string AMPERSAND = "&";
        private const string PAGE_FORMAT = "page={0}";
        private const string PAGE_SIZE_FORMAT = "pageSize={0}";
        private const string QUESTION_MARK = "?";
        private const string SORTDIR = "sortDir=";
        private const string SORTDIR_ASC = SORTDIR + "asc";
        private const string SORTDIR_DESC = SORTDIR + "desc";

        protected abstract string Endpoint { get; }

        #endregion

        [Parameter(Mandatory = false)]
        public int Page = 1;

        [Parameter(Mandatory = false)]
        public int PageSize = 10;

        [Parameter(Mandatory = false)]
        public SortDirection SortDirection = SortDirection.Ascending;

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _list = new List<string>();
        }

        protected override void ProcessRecord()
        {
            if (base.HasParameterSpecified(this, x => x.Page))
                _list.Add(string.Format(PAGE_FORMAT, this.Page));

            if (base.HasParameterSpecified(this, x => x.PageSize))
                _list.Add(string.Format(PAGE_SIZE_FORMAT, this.PageSize));

            if (base.HasParameterSpecified(this, x => x.SortDirection))
            {
                if (this.SortDirection == SortDirection.Ascending)
                    _list.Add(SORTDIR_ASC);

                else
                    _list.Add(SORTDIR_DESC);
            }

            if (_list.Count > 0)
                _ep = this.Endpoint + QUESTION_MARK + string.Join(AMPERSAND, _list);

            else
                _ep = this.Endpoint;

            string jsonRes = base.SendSonarrRawGet(_ep);
            if (!string.IsNullOrEmpty(jsonRes))
            {
                base.WriteObject(JsonObject.ConvertFromJson(jsonRes, out ErrorRecord er));
            }
        }
    }
}
