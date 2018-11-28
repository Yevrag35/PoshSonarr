using Sonarr.Api.Endpoints;
using Sonarr.Api.Results;
using System;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SonarrCalendar", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(CalendarEntry))]
    public class GetSonarrCalendar : BaseCmdlet
    {
        [Parameter(Mandatory = false, Position = 0)]
        public DateTime? Start = null;

        [Parameter(Mandatory = false, Position = 1)]
        public DateTime? End = null;

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            var cal = new Calendar(Start, End);

            var result = Api.Send(cal);
            PipeBack<CalendarEntry>(result);
        }
    }
}
