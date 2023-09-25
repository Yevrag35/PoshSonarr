using MG.Sonarr.Next.Services;
using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http;
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

        [Parameter(Mandatory = false, Position = 0)]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, Position = 1)]
        public DateTime EndDate
        {
            get => _end ?? this.StartDate.AddDays(7d);
            set => _end = value;
        }

        [Parameter(Mandatory = true, ParameterSetName = "ShowToday")]
        public SwitchParameter Today { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ShowTomorrow")]
        public SwitchParameter Tomorrow { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter IncludeUnmonitored { get; set; }

        protected override void Begin()
        {
            //if (this.HasParameter(x => x.)
        }

        protected override void Process()
        {
            var parameters = GetParameters(this.StartDate, this.EndDate, this.IncludeUnmonitored);
            string url = GetUrl(parameters);

            var response = this.SendGetRequest<List<PSObject>>(url);
            if (response.IsError)
            {
                this.StopCmdlet(response.Error);
            }
            else
            {
                this.WriteCollection(response.Data);
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
                { START, start, 19 },
                { END, end, 19 },
                { nameof(unmonitored), unmonitored },
            };
        }
    }
}
