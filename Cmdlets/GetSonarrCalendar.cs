using MG.Api;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SonarrCalendar", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(CalendarEntry))]
    public class GetSonarrCalendar : PSCmdlet
    {
        private ApiCaller api;

        [Parameter(Mandatory = false, Position = 0)]
        public DateTime? Start = null;

        [Parameter(Mandatory = false, Position = 1)]
        public DateTime? End = null;

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            if (!SonarrServiceContext.IsSet)
                throw new SonarrContextNotSetException("  Run the 'Connect-Sonarr' cmdlet first.");

            api = new ApiCaller(SonarrServiceContext.Value);
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            var cal = new Calendar(Start, End);

            var result = api.Send(cal, SonarrServiceContext.ApiKey);
            for (int i = 0; i < result.Count; i++)
            {
                Dictionary<object, object> d = result[i];
                WriteObject((CalendarEntry)d);
            }
        }
    }
}
