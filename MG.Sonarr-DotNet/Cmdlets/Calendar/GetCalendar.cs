using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Helpers;
using MG.Sonarr.Functionality.Strings;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private bool _nextWeek;
        private bool _thisWeek;
        private bool _today;
        private bool _tomorrow;
        private bool _yesterday;
        private DateRange _dateRange;

        #endregion

        #region PARAMETERS
        /// <summary>
        ///     <para type="description">The start date to retrieve calendar entries from.</para>
        /// </summary>
        [Parameter(Mandatory = false, Position = 0)]
        public DateTime StartDate
        {
            get => default;
            set
            {
                if (value.Equals(DateTime.MinValue))
                    value = DateTime.Now;

                _dateRange = new DateRange(value);
            }
        }

        /// <summary>
        /// <para type="description">The end date to retrieve calendar entries from.</para>
        /// </summary>
        [Parameter(Mandatory = false, Position = 1)]
        public DateTime EndDate
        {
            get => default;
            set
            {
                if (value.Equals(DateTime.MinValue))
                    value = DateTime.Now.AddDays(7);

                _dateRange.AddEndTime(value);
            }
        }

        /// <summary>
        /// <para type="description">Specifies the DayOfWeeks to get entries in the specified date range.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public DayOfWeek[] DayOfWeek { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ShowThisWeek")]
        public SwitchParameter ThisWeek
        {
            get => _thisWeek;
            set
            {
                _thisWeek = value;
                if (_thisWeek)
                {
                    _dateRange = DateRange.ThisWeek();
                }
            }
        }

        [Parameter(Mandatory = true, ParameterSetName = "ShowNextWeek")]
        public SwitchParameter NextWeek
        {
            get => _nextWeek;
            set
            {
                _nextWeek = value;
                if (_nextWeek)
                {
                    _dateRange = DateRange.NextWeek();
                }
            }
        }

        [Parameter(Mandatory = true, ParameterSetName = "ShowToday")]
        public SwitchParameter Today
        {
            get => _today;
            set
            {
                _today = value;
                if (_today)
                    _dateRange = DateRange.Today();
            }
        }

        [Parameter(Mandatory = true, ParameterSetName = "ShowTomorrow")]
        public SwitchParameter Tomorrow
        {
            get => _tomorrow;
            set
            {
                _tomorrow = value;
                if (_tomorrow)
                    _dateRange = DateRange.Tomorrow();
            }
        }

        [Parameter(Mandatory = true, ParameterSetName = "ShowYesterday")]
        public SwitchParameter Yesterday
        {
            get => _yesterday;
            set
            {
                _yesterday = value;
                if (_yesterday)
                    _dateRange = DateRange.Yesterday();
            }
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
        }

        protected override void ProcessRecord()
        {
            if (null == _dateRange)
                _dateRange = new DateRange();

            IUrlParameter[] parameters = _dateRange.AsUrlParameters();

            List<CalendarEntry> entries = this.GetCalendarEntries(Endpoint.Calendar.WithQuery(parameters));

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
        private List<CalendarEntry> GetCalendarEntries(Endpoint endpoint) => base.SendSonarrListGet<CalendarEntry>(endpoint);

        #endregion
    }
}