using MG.Sonarr.Next.Services;
using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Calendar;
using MG.Sonarr.Next.Shell.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Shell.Cmdlets.Calendar
{
    [Cmdlet(VerbsCommon.Get, "SonarrCalendar", DefaultParameterSetName = "None")]
    public sealed class GetSonarrCalendarCmdlet : SonarrApiCmdletBase
    {
        const string START = "start";
        const string END = "end";
        DateTime? _end;
        readonly HashSet<DayOfWeek> _dows;

        public GetSonarrCalendarCmdlet()
            : base()
        {
            _dows = new(1);
        }

        [Parameter(Mandatory = false, Position = 0)]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 1)]
        public DateTime EndDate
        {
            get => _end ?? this.StartDate.AddDays(7d);
            set => _end = value;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false)]
        public DayOfWeek[] DayOfWeek
        {
            get => Array.Empty<DayOfWeek>();
            set => _dows.UnionWith(value);
        }

        [Parameter(Mandatory = true, ParameterSetName = "ShowToday")]
        public SwitchParameter Today { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ShowTomorrow")]
        public SwitchParameter Tomorrow { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter IncludeUnmonitored { get; set; }

        protected override void Begin()
        {
            if (this.HasParameter(x => x.Today))
            {
                this.StartDate = DateTime.Today;
                this.EndDate = this.StartDate.AddDays(1d).AddSeconds(-1d);
            }
            else if (this.HasParameter(x => x.Tomorrow))
            {
                this.StartDate = DateTime.Today.AddDays(1d);
                this.EndDate = this.StartDate.AddDays(1d).AddSeconds(-1d);
            }
        }

        protected override void Process()
        {
            var parameters = GetParameters(this.StartDate, this.EndDate, this.IncludeUnmonitored);
            string url = GetUrl(parameters);

            var response = this.SendGetRequest<MetadataList<CalendarObject>>(url);
            if (response.IsError)
            {
                this.StopCmdlet(response.Error);
                return;
            }
            else if (this.HasParameter(x => x.DayOfWeek) && _dows.Count > 0)
            {
                FilterByDayOfWeek(response.Data, _dows);
            }

            this.WriteCollection(response.Data);
        }

        private static void FilterByDayOfWeek(List<CalendarObject> list, IReadOnlySet<DayOfWeek> dows)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!dows.Contains(list[i].AirDateUtc.DayOfWeek))
                {
                    list.RemoveAt(i);
                }
            }
        }
        private static string GetUrl(QueryParameterCollection col)
        {
            ReadOnlySpan<char> baseUrl = Constants.CALENDAR;
            Span<char> span = stackalloc char[baseUrl.Length + col.MaxLength + 1];

            int position = 0;
            baseUrl.CopyToSlice(span, ref position);
            span[position++] = '?';
            _ = col.TryFormat(span.Slice(position), out int written, Constants.CALENDAR_DT_FORMAT, Statics.DefaultProvider);

            return new string(span.Slice(0, position + written));
        }
        private static QueryParameterCollection GetParameters(DateTime start, DateTime end, bool unmonitored)
        {
            return new(3)
            {
                { nameof(unmonitored), unmonitored },
                { START, start, Constants.CALENDAR_DT_FORMAT.Length },
                { END, end, Constants.CALENDAR_DT_FORMAT.Length },
            };
        }
    }
}
