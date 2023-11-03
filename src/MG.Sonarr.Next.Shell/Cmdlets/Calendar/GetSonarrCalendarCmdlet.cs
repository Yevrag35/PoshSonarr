using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Calendar;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Shell.Output;

namespace MG.Sonarr.Next.Shell.Cmdlets.Calendar
{
    [Cmdlet(VerbsCommon.Get, "SonarrCalendar", DefaultParameterSetName = "None")]
    [OutputType(typeof(ICalendarOutput))]
    public sealed class GetSonarrCalendarCmdlet : SonarrApiCmdletBase
    {
        const string START = "start";
        const string END = "end";
        static readonly TimeSpan WEEK_TIME_SPAN = TimeSpan.FromDays(8).Subtract(TimeSpan.FromSeconds(1));

        DateTime? _end;
        HashSet<DayOfWeek> _dows = null!;
        MetadataTag _tag = null!;

        [Parameter(Position = 0)]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Position = 1)]
        public DateTime EndDate
        {
            get => _end ?? this.StartDate.Add(WEEK_TIME_SPAN);
            set => _end = value;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter]
        public DayOfWeek[] DayOfWeek
        {
            get => Array.Empty<DayOfWeek>();
            set => _dows.UnionWith(value);
        }

        [Parameter(Mandatory = true, ParameterSetName = "ShowToday")]
        public SwitchParameter Today { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ShowTomorrow")]
        public SwitchParameter Tomorrow { get; set; }

        // Possibly coming in v4
        //[Parameter]
        //public IntOrString[] Tag { get; set; } = Array.Empty<IntOrString>();

        [Parameter]
        public SwitchParameter IncludeEpisodeFile { get; set; }

        [Parameter]
        public SwitchParameter IncludeEpisodeImages { get; set; }

        [Parameter]
        public SwitchParameter IncludeSeries { get; set; }

        [Parameter]
        public SwitchParameter IncludeUnmonitored { get; set; }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _dows = new(1);
            _tag = provider.GetRequiredService<IMetadataResolver>()[Meta.CALENDAR];
        }

        protected override void Begin(IServiceProvider provider)
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

        protected override void Process(IServiceProvider provider)
        {
            var parameters = GetParameters(this.StartDate, this.EndDate, this.IncludeUnmonitored, this.IncludeEpisodeFile, this.IncludeEpisodeImages, this.IncludeSeries);
            string url = _tag.GetUrl(parameters);

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

        private static void FilterByDayOfWeek(IList<CalendarObject> list, IReadOnlySet<DayOfWeek> dows)
        {
            int removed = list.RemoveWhere(dows, predicate: (item, state) =>
            {
                return !state!.Contains(item.AirDateUtc.DayOfWeek);
            });

            Debug.WriteLine($"Filtered {removed} items from {nameof(list)}.");
        }
        private static QueryParameterCollection GetParameters(DateTime start, DateTime end, bool unmonitored, bool includeEpisodeFile, bool includeEpisodeImages, bool includeSeries)
        {
            int numberOfParameters = 6;
            QueryParameterCollection col = new(numberOfParameters)
            {
                { nameof(unmonitored), unmonitored },
                { nameof(includeEpisodeFile), includeEpisodeFile },
                { nameof(includeSeries), includeSeries },
                { nameof(includeEpisodeImages), includeEpisodeImages },
                { START, start, Constants.CALENDAR_DT_FORMAT.Length, Constants.CALENDAR_DT_FORMAT },
                { END, end, Constants.CALENDAR_DT_FORMAT.Length, Constants.CALENDAR_DT_FORMAT },
            };

            Debug.Assert(col.Count == numberOfParameters);
            return col;
        }
    }
}
