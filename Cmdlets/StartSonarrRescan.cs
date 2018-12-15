using MG.Api;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System;
using System.Linq;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Start, "SonarrRescan", SupportsShouldProcess = true,
        DefaultParameterSetName = "ByPipeline")]
    [CmdletBinding(PositionalBinding = false)]
    public class StartSonarrRescan : CommandsWithSeries
    {
        private const string SERIES_ID = "seriesId";

        internal override SonarrCommand Command => SonarrCommand.RescanSeries;

        //internal override CommandResult Result { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByPipeline",
            ValueFromPipeline = true, DontShow = true)]
        public SeriesResult Series { get; set; }

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (!MyInvocation.BoundParameters.ContainsKey("Series"))
                base.ProcessRecord();
            else
                _list.Add(Series);
        }

        protected override void EndProcessing()
        {
            for (int i = 0; i < _list.Count; i++)
            {
                var ser = _list[i];
                var cmd = new Command(Command);
                cmd.Parameters.Add(SERIES_ID, Convert.ToString(ser.Id));

                var initialCmd = Api.SonarrPostAs<CommandResult>(cmd).ToArray()[0];
                if (_wait)
                {
                    var wait = WaitTilComplete(initialCmd.Id, TimeOut);
                    WriteObject(wait);
                }
                else
                    WriteObject(initialCmd);
            }
        }
    }
}
