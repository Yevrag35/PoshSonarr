using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Functionality.Url;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Log", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByPaging")]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(LogRecord))]
    public class GetLog : BaseSonarrCmdlet// : LazySonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private bool _all;
        private const int DEFAULT_PAGE_SIZE = 10;
        private const string ENDPOINT = "/log";
        private const int MAX_TOP_SIZE = 1000000;
        private const int START_ALL = 1;
        private const string URI_FORMAT = "{0}{1}";
        private IUrlParameterCollection UrlParameters { get; } = new UrlParameterCollection();


        #endregion

        #region PARAMETERS

        [Parameter(Mandatory = false, Position = 0)]
        public string Severity = "All";

        [Parameter(Mandatory = false, Position = 1)]
        public LogSortKey SortBy = LogSortKey.Time;

        [Parameter(Mandatory = false, Position = 2)]
        public SortDirection SortDirection = SortDirection.Descending;

        [Parameter(Mandatory = true, ParameterSetName = "ByPagingIncludeFiltering")]
        [Parameter(Mandatory = true, ParameterSetName = "ByTopIncludeFiltering")]
        [ValidateSet("ExceptionType", "Level")]
        public string FilterBy { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByPagingIncludeFiltering")]
        [Parameter(Mandatory = true, ParameterSetName = "ByTopIncludeFiltering")]
        public IConvertible FilterValue { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = "ByPaging")]
        [Parameter(Mandatory = false, ParameterSetName = "ByPagingIncludeFiltering")]
        public int Page = PagingParameter.DefaultPage;

        [Parameter(Mandatory = false, ParameterSetName = "ByPaging")]
        [Parameter(Mandatory = false, ParameterSetName = "ByPagingIncludeFiltering")]
        public int PageSize = PagingParameter.DefaultPageSize;

        [Parameter(Mandatory = true, ParameterSetName = "ByTop")]
        [Parameter(Mandatory = true, ParameterSetName = "ByTopIncludeFiltering")]
        [ValidateRange(1, MAX_TOP_SIZE)]
        public int Top { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByAll")]
        public SwitchParameter All
        {
            get => _all;
            set => _all = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (this.ContainsParameter(x => x.Severity) && "All".Equals(Severity, StringComparison.InvariantCultureIgnoreCase))
            {
                this.FilterBy = LogSortKey.Level.ToString();
                this.FilterValue = this.Severity;
            }
        }

        protected override void ProcessRecord()
        {
            this.AddUrlParameters();
            string url = this.MakeUrl();

            RecordSummary summary = base.SendSonarrGet<RecordSummary>(url);
            if (summary != null && summary.TotalRecords > 0)
            {
                base.SendToPipeline(summary.Records);
            }
        }

        #endregion

        #region BACKEND METHODS
        private void AddUrlParameters()
        {
            this.SetFilterParameter();
            this.SetPagingParameters();
            this.UrlParameters.Add(new LogSortParameter(this.SortBy, this.SortDirection));
        }
        private string MakeUrl() => string.Format(URI_FORMAT, ENDPOINT, this.UrlParameters.ToQueryString());
        private void SetFilterParameter()
        {
            if (!string.IsNullOrEmpty(this.FilterBy))
            {
                this.UrlParameters.Add(new FilterLogParameter(this.FilterBy, this.FilterValue));
            }
        }
        private void SetPagingParameters()
        {
            if (this.ContainsAnyParameters(x => x.Page, x => x.PageSize))
            {
                this.UrlParameters.Add(new PagingParameter(this.Page, this.PageSize));
            }
            else if (_all)
            {
                string checkUri = string.Format(URI_FORMAT, ENDPOINT, "?pageSize=1");
                RecordSummary checkSum = base.SendSonarrGet<RecordSummary>(checkUri);
                if (checkSum != null)
                {
                    this.Top = checkSum.TotalRecords;
                }

                this.UrlParameters.Add(new PagingParameter(PagingParameter.DefaultPage, this.Top));
            }
            else if (this.ContainsParameter(x => x.Top))
            {
                this.UrlParameters.Add(new PagingParameter(PagingParameter.DefaultPage, this.Top));
            }
        }

        #endregion
    }
}