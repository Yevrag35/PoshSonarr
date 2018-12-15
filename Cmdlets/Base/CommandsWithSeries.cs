using MG.Api;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;

namespace Sonarr.Api.Cmdlets
{
    [CmdletBinding(PositionalBinding = false)]
    public abstract class CommandsWithSeries : GetSonarrSeries
    {
        internal abstract SonarrCommand Command { get; }
        internal abstract KeyValuePair<string, object> CommandKVP { get; set; }

        protected private bool _wait;
        [Parameter(Mandatory = false)]
        public SwitchParameter WaitForCompletion
        {
            get => _wait;
            set => _wait = value;
        }

        [Parameter(Mandatory = false)]
        public int TimeOut = 60;

        private bool _force;
        [Parameter(Mandatory = false, DontShow = true)]
        public SwitchParameter Force
        {
            get => _force;
            set => _force = value;
        }

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void EndProcessing()
        {

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
