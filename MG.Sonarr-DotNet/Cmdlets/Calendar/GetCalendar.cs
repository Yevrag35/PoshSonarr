using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Calendar", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "None")]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(CalendarEntry))]
    public class GetCalendar : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string DT_FORMAT = "yyyy-MM-dd";
        private const string EP = "/calendar";
        private const string EP_WITH_DATE = EP + "?start={0}&end={1}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0)]
        public DateTime StartDate = DateTime.Now;

        [Parameter(Mandatory = false, Position = 1)]
        public DateTime EndDate = DateTime.Now.AddDays(7);

        [Parameter(Mandatory = true, ParameterSetName = "ByDayOfWeek")]
        public DayOfWeek[] DayOfWeek { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "BySeriesTitle")]
        [Alias("Series")]
        [SupportsWildcards]
        public string SeriesTitle { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (this.MyInvocation.BoundParameters.ContainsKey("StartDate") &&
                !this.MyInvocation.BoundParameters.ContainsKey("EndDate"))
                this.EndDate = this.StartDate.AddDays(7);
        }

        protected override void ProcessRecord()
        {
            string start = this.DateToString(this.StartDate);
            string end = this.DateToString(this.EndDate);
            string full = string.Format(EP_WITH_DATE, start, end);

            string jsonRes = base.TryGetSonarrResult(full);
            if (!string.IsNullOrEmpty(jsonRes))
            {
                List<CalendarEntry> entries = SonarrHttp.ConvertToSonarrResults<CalendarEntry>(jsonRes, out bool iso);
                if (this.ParameterSetName == "ByDayOfWeek")
                {
                    base.WriteObject(entries.FindAll(x => x.DayOfWeek.HasValue && this.DayOfWeek.Contains(x.DayOfWeek.Value)), true);
                }
                else if (this.ParameterSetName == "BySeriesTitle")
                {
                    var wcp = new WildcardPattern(this.SeriesTitle, WildcardOptions.IgnoreCase);
                    base.WriteObject(entries.FindAll(x => wcp.IsMatch(x.Series)), true);
                }
                else
                {
                    base.WriteObject(entries, true);
                }
            }
        }

        #endregion

        #region METHODS
        private string DateToString(DateTime dt) => dt.ToString(DT_FORMAT);

        #endregion
    }
}