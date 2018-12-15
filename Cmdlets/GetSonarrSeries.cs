using MG.Api;
using Sonarr.Api.Cmdlets.Base;
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
        protected private List<SeriesResult> _list;

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "BySeriesTitle")]
        [SupportsWildcards()]
        [Alias("st", "Title", "Name")]
        public string SeriesName { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = "BySeriesId")]
        public int? SeriesId { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _list = new List<SeriesResult>();
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            int? sid = null;
            if (SeriesId.HasValue)
                sid = SeriesId;
            GetSeries(sid);

            if (MyInvocation.BoundParameters.ContainsKey("SeriesName"))
            {
                FilterByName(SeriesName);
            }
        }

        protected override void EndProcessing() => WriteObject(_list.ToArray(), true);

        private void GetSeries(int? seriesId) =>
            _list.AddRange(Api.SonarrGetAs<SeriesResult>(new Series(seriesId)));

        private void FilterByName(string name)
        {
            var wco = WildcardOptions.IgnoreCase;
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                var ser = _list[i];
                var wcp = new WildcardPattern(name, wco);
                if (!wcp.IsMatch(ser.Title))
                    _list.Remove(ser);
            }
        }
    }
}
