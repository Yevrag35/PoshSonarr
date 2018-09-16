using MG.Api;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Start, "SonarrRescan", SupportsShouldProcess = true,
        DefaultParameterSetName = "ByPipeline")]
    [CmdletBinding(PositionalBinding = false)]
    public class StartSonarrRescan : BaseCommandCmdlet
    {
        public override SonarrCommand Command => SonarrCommand.RescanSeries;
        public override SonarrMethod Method => SonarrMethod.POST;

        [Parameter(Mandatory = false, ParameterSetName = "ByPipeline",
            ValueFromPipeline = true, DontShow = true)]
        public SeriesResult Series { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "BySeriesId", Position = 0)]
        public long SeriesId { get; }

        private bool _force;
        [Parameter(Mandatory = false, DontShow = true)]
        public SwitchParameter Force
        {
            get => _force;
            set => _force = value;
        }

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
            switch (ParameterSetName)
            {
                case "ByPipeline":
                    if (Series == null)
                    {
                        if (ShouldContinue("All Sonarr Series", "Rescan all series?  This may produce a lot of disk activity.") ||
                                _force)
                        {
                            result = ApplyCommandToAll();
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        result = ApplyCommandToOne(new object[2] { "seriesId", Series.Id });
                    }                    break;
                case "BySeriesId":
                    result = ApplyCommandToOne(new object[2] { "seriesId", SeriesId });
                    break;
            }
            PipeBackResult(result);
        }
    }
}
