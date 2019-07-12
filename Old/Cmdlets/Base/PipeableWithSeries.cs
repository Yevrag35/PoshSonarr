using Sonarr.Api.Results;
using System.Collections.Generic;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets.Base
{
    public abstract class PipeableWithSeries : GetSonarrSeries
    {
        [Parameter(Mandatory = true, ParameterSetName = "BySeriesPipeline",
            ValueFromPipeline = true)]
        public SeriesResult Series { get; set; }

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (!MyInvocation.BoundParameters.ContainsKey("Series"))
                base.ProcessRecord();
            else
                _list.Add(Series);
        }

        protected override void EndProcessing() { }
    }
}
