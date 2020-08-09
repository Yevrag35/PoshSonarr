using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Functionality.Results;
using MG.Sonarr.Functionality.Strings;
using MG.Sonarr.Functionality.Url;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "EpisodeHistory")]
    public class GetHistory : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        IUrlParameterCollection paramCol;
        List<string> _allQueries;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public long[] EpisodeId { get; set; }

        [Parameter(Mandatory = false)]
        [ValidateRange(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Parameter(Mandatory = false)]
        [ValidateRange(1, int.MaxValue)]
        public int PageSize { get; set; } = 10;

        [Parameter(Mandatory = false)]
        [ValidateSet("Date", "Series-Title")]
        public string SortKey { get; set; } = "Date";

        [Parameter(Mandatory = false)]
        public SortDirection SortDirection { get; set; } = SortDirection.Descending;

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            paramCol = new UrlParameterCollection(2)
            {
                new PagingParameter(this.PageNumber, this.PageSize),
                new HistorySortParameter(this.GetKeyFromString(this.SortKey), this.SortDirection)
            };
            _allQueries = new List<string>(1);
        }

        protected override void ProcessRecord()
        {
            if (! this.ContainsParameter(x => x.EpisodeId))
            {
                string oneQuery = paramCol.ToQueryString();
                _allQueries.Add(oneQuery);
            }
            else
            {
                _allQueries.Capacity = this.EpisodeId.Length;
                for (int i = 0; i < this.EpisodeId.Length; i++)
                {
                    string query = paramCol.ToQueryString(new EpisodeIdParameter(this.EpisodeId[i]));
                    _allQueries.Add(query);
                }
            }
        }

        protected override void EndProcessing()
        {
            for (int i = 0; i < _allQueries.Count; i++)
            {
                string url = this.ToUrl(_allQueries[i]);
                HistoryRecordPage page = base.SendSonarrGet<HistoryRecordPage>(url);
                if (page != null && page.Records.Count > 0)
                    base.WriteObject(page.Records, true);
            }
        }

        #endregion

        #region BACKEND METHODS
        private HistorySortKey GetKeyFromString(string key)
        {
            string lower = key.ToLower();

            switch (lower)
            {
                case "series-title":
                    return HistorySortKey.SeriesTitle;

                default:
                    return HistorySortKey.Date;
            }
        }

        private string ToUrl(string query) => string.Format("{0}{1}", ApiEndpoints.History, query);

        #endregion
    }
}