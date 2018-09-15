using MG.Api;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SonarrSeries", ConfirmImpact = ConfirmImpact.None,
        DefaultParameterSetName = "BySeriesTitle")]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(SeriesResult))]
    public class GetSonarrSeries : BaseCmdlet
    {
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "BySeriesTitle")]
        [Alias("st", "Title")]
        public string SeriesTitle { get; set; }

        private bool _ex;
        [Parameter(Mandatory = false, ParameterSetName = "BySeriesTitle")]
        public SwitchParameter Exact
        {
            get => _ex;
            set => _ex = value;
        }

        [Parameter(Mandatory = false, ParameterSetName = "BySeriesId")]
        public int? SeriesId { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            if (!SonarrServiceContext.IsSet)
                throw new SonarrContextNotSetException("  Run the 'Connect-Sonarr' cmdlet first.");

            Api = new ApiCaller(SonarrServiceContext.Value);
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            if (ParameterSetName == "BySeriesTitle")
                SeriesId = null;

            // Make Series object
            var series = new Series(SeriesId);

            if (SonarrServiceContext.SonarrSeries == null)
            {
                SonarrServiceContext.SonarrSeries = GetAllSeries(series);
            }

            if (!string.IsNullOrEmpty(SeriesTitle))
            {
                for (int i = 0; i < SonarrServiceContext.SonarrSeries.Count; i++)
                {
                    IDictionary d = SonarrServiceContext.SonarrSeries[i];
                    string tit = (string)d["title"];
                    if (!_ex && tit.IndexOf(SeriesTitle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        _ex && tit.Equals(SeriesTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        var sr = new SeriesResult(d);
                        WriteObject(sr);
                    }
                }
            }
            else
            {
                PipeBack<SeriesResult>(SonarrServiceContext.SonarrSeries);
            }
        }

        private ApiResult GetAllSeries(Series ep)
        {
            return Api.Send(ep);
        }
    }
}
