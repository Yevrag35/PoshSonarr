using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
#if (NETCOREAPP == false)
    /// <summary>
    ///     <para type="synopsis">Retrieves Sonarr calendar entries.</para>
    ///     <para type="description">
    ///         Gets the calendar schedule for the specified date range.  By default, the range
    ///         retrieved is between now and 7 days out.
    ///     </para>
    /// </summary>
    /// <example>
    ///     <code>Get-SonarrCalendar</code>
    /// </example>
    /// <example>
    ///     <code>Get-SonarrCalendar -DayOfWeek Friday</code>
    /// </example>
#endif
    [Cmdlet(VerbsCommon.Get, "Calendar", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "None")]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(CalendarEntry))]
    public class GetCalendar : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string DT_FORMAT = "yyyy-MM-dd";
        private const string EP = "/calendar";
        private const string EP_WITH_DATE = EP + "?start={0}&end={1}";

        private bool _today;
        private bool _tomorrow;

        #endregion

        #region PARAMETERS
        /// <summary>
        ///     <para type="description">The start date to retrieve calendar entries from.</para>
        /// </summary>
        [Parameter(Mandatory = false, Position = 0)]
        public DateTime StartDate = DateTime.Now;

        /// <summary>
        /// <para type="description">The end date to retrieve calendar entries from.</para>
        /// </summary>
        [Parameter(Mandatory = false, Position = 1)]
        public DateTime EndDate = DateTime.Now.AddDays(7);

        /// <summary>
        /// <para type="description">Specifies the DayOfWeeks to get entries in the specified date range.</para>
        /// </summary>
        [Parameter(Mandatory = true)]//, ParameterSetName = "ByDayOfWeek")]
        public DayOfWeek[] DayOfWeek { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ShowToday")]
        public SwitchParameter Today
        {
            get => _today;
            set => _today = value;
        }

        [Parameter(Mandatory = true, ParameterSetName = "ShowTomorrow")]
        public SwitchParameter Tomorrow
        {
            get => _tomorrow;
            set => _tomorrow = value;
        }

        /// <summary>
        /// <para type="description">Return only the specified series from the calendar.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "BySeriesTitle")]
        [Alias("Series")]
        [SupportsWildcards]
        public string[] SeriesTitle { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (base.HasParameterSpecified(this, x => x.StartDate) && ! base.HasParameterSpecified(this, x => x.EndDate))
            {
                this.EndDate = this.StartDate.AddDays(7);
            }
            else if (base.HasParameterSpecified(this, x => x.Today))
            {
                this.StartDate = DateTime.Today;
                this.EndDate = DateTime.Today.AddDays(1);
            }
            else if (base.HasParameterSpecified(this, x => x.Tomorrow))
            {
                this.StartDate = DateTime.Today.AddDays(1);
                this.EndDate = DateTime.Today.AddDays(2);
            }
        }

        protected override void ProcessRecord()
        {
            string start = this.DateToString(this.StartDate);
            string end = this.DateToString(this.EndDate);
            string full = string.Format(EP_WITH_DATE, start, end);

            List<CalendarEntry> entries = this.GetCalendarEntries(full);

            if (base.HasParameterSpecified(this, x => x.DayOfWeek))
            {
                base.SendToPipeline(entries.FindAll(x => x.DayOfWeek.HasValue && this.DayOfWeek.Contains(x.DayOfWeek.Value)));
            }
            else
            {
                base.SendToPipeline(base.FilterByStringParameter(entries, e => e.Series, this, cmd => cmd.SeriesTitle));
            }
        }

        #endregion

        #region METHODS
        private List<CalendarEntry> GetCalendarEntries(string uri) => base.SendSonarrListGet<CalendarEntry>(uri);

        private string DateToString(DateTime dt) => dt.ToString(DT_FORMAT);

        #endregion
    }
}