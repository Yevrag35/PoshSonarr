using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
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
        [Parameter(Mandatory = false)]
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
            if (this.ContainsParameter(x => x.DayOfWeek) && !this.ContainsParameter(x => x.StartDate))
            {
                this.StartDate = DateTime.Today;
                if (!this.ContainsParameter(x => x.EndDate))
                {
                    this.EndDate = this.StartDate.AddDays(8).AddSeconds(-1);
                }
            }

            if (this.ContainsParameter(x => x.StartDate) && ! this.ContainsParameter(x => x.EndDate))
            {
                this.EndDate = this.StartDate.AddDays(7);
            }
            else if (_today)
            {
                this.SetOneDayRange(DateTime.Today);
            }
            else if (_tomorrow)
            {
                this.SetOneDayRange(DateTime.Today.AddDays(1));
            }
        }

        protected override void ProcessRecord()
        {
            string start = this.DateToString(this.StartDate);
            string end = this.DateToString(this.EndDate);

            string full = string.Format(ApiEndpoints.Calendar_WithDate, start, end);

            List<CalendarEntry> entries = this.GetCalendarEntries(full);

            if (this.ContainsParameter(x => x.DayOfWeek))
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

        private string DateToString(DateTime dt) => dt.ToString(ApiEndpoints.Calendar_DTFormat);

        private void SetOneDayRange(DateTime beginning)
        {
            this.StartDate = beginning;
            this.EndDate = beginning.AddDays(1).AddSeconds(-1);
        }

        #endregion
    }
}