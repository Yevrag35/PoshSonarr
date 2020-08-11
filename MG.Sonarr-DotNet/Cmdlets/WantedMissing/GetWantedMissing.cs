using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Functionality.Url;
using MG.Sonarr.Results;
using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "WantedMissing", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    public class GetWantedMissing : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        //protected override string Endpoint => "/wanted/missing";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false)]
        [ValidateRange(1, int.MaxValue)]
        public int PageSize { get; set; } = 10;

        [Parameter(Mandatory = false)]
        [ValidateRange(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        [Parameter(Mandatory = false)]
        public WantedMissingSortKey SortKey { get; set; } = WantedMissingSortKey.AirDateUtc;

        [Parameter(Mandatory = false)]
        public SortDirection SortDirection { get; set; } = SortDirection.Descending;

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            var pageParam = new PagingParameter(this.PageNumber, this.PageSize);
            var sortParam = new WantedMissingSortParameter(this.SortKey, this.SortDirection);

            Endpoint endpoint = Endpoint.WantedMissing.WithQuery(pageParam, sortParam);
            WantedMissingPage page = base.SendSonarrGet<WantedMissingPage>(endpoint);
            if (page != null && page.Records.Count > 0)
                base.WriteObject(page.Records, true);
        }

        #endregion
    }
}