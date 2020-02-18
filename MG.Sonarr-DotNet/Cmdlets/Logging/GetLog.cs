using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Log", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    public class GetLog : BaseSonarrCmdlet// : LazySonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        //protected override string Endpoint => "/log";
        private const string ENDPOINT = "/log";
        //private string _sortKey;

        #endregion

        #region PARAMETERS

        [Parameter(Mandatory = false, Position = 1)]
        public LogLevel Severity { get; set; }

        [Parameter(Mandatory = false, Position = 2)]
        public LogSortKey SortBy = LogSortKey.Time;

        [Parameter(Mandatory = false, Position = 3)]
        public SortDirection SortDirection = SortDirection.Descending;

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            IUrlParameterCollection col = new UrlParameterCollection();
            if (base.HasParameterSpecified(this, x => x.Severity))
            {
                col.Add(new FilterParameter("level", this.Severity));
            }
            col.Add(new LogSortParameter(this.SortBy, this.SortDirection));

            string url = string.Format("{0}{1}", ENDPOINT, col.ToQueryString());

            RecordSummary summary = base.SendSonarrGet<RecordSummary>(url);
            if (summary != null && summary.TotalRecords > 0)
            {
                base.SendToPipeline(summary.Records);
            }
        }

        //protected override void ProcessRecord()
        //{
        //    if (base.HasParameterSpecified(this, x => x.Severity))
        //    {
        //        _list.Add("filterKey=level");
        //        _list.Add(string.Format("filterValue={0}", this.Severity.ToString()));
        //    }
        //    base.ProcessRecord();
        //}

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}