using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;
using System.Threading;

namespace Sonarr.Api.Cmdlets.Base
{
    [CmdletBinding(PositionalBinding = false)]
    public abstract class PipeableWithSeriesCommand : PipeableWithSeries
    {
        internal const string SERIES_ID = "seriesId";

        internal abstract SonarrCommand Command { get; }

        protected private bool _wait;
        [Parameter(Mandatory = false)]
        [Alias("wait", "w")]
        public SwitchParameter WaitForCompletion
        {
            get => _wait;
            set => _wait = value;
        }

        [Parameter(Mandatory = false)]
        public int TimeOut = 60;

        private bool _force;
        [Parameter(Mandatory = false)]
        [Alias("f")]
        public SwitchParameter Force
        {
            get => _force;
            set => _force = value;
        }

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord() => base.ProcessRecord();

        protected override void EndProcessing() { }

        internal SonarrResult ProcessCommand(IDictionary parameters)
        {
            var cmd = new Command(Command);
            if (parameters != null)
            {
                var keys = parameters.Keys.Cast<string>().ToArray();
                for (int i = 0; i < keys.Length; i++)
                {
                    var key = keys[i];
                    var value = parameters[key];
                    cmd.Parameters.Add(key, value);
                }
            }
            var initialCmd = Api.SonarrPostAs<CommandResult>(cmd).ToArray()[0];
            if (_wait)
            {
                var wait = WaitTilComplete(initialCmd.Id, TimeOut);
                return wait;
            }
            else
                return initialCmd;
        }

        internal FinalCommandResult WaitTilComplete(long id, int timeout)
        {
            var cmd = new Command(id);
            int i = 0;
            while (i < (timeout / 2))
            {
                var result = Api.SonarrGetAs<FinalCommandResult>(cmd).ToArray()[0];
                if (result.Status.Equals("completed", StringComparison.OrdinalIgnoreCase))
                {
                    return result;
                }
                i++;
                Thread.Sleep(2000);
            }
            return null;
        }
    }
}
