using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Calendar;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Shell.Output;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Calendar
{
    [Cmdlet(VerbsCommon.Get, "SonarrCalendar", DefaultParameterSetName = "None")]
    [OutputType(typeof(ICalendarOutput))]
    public sealed class GetSonarrCalendarCmdlet : SonarrMetadataCmdlet
    {
        const string START = "start";
        const string END = "end";
        static readonly TimeSpan WEEK_TIME_SPAN = TimeSpan.FromDays(8).Subtract(TimeSpan.FromSeconds(1));

        DateTime? _end;
        QueryCol _queryCol = null!;

        [Parameter(Position = 0)]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Position = 1)]
        public DateTime EndDate
        {
            get => _end ??= this.StartDate.Add(WEEK_TIME_SPAN);
            set => _end = value;
        }

        [Parameter]
        [DistinctValues(typeof(DayOfWeek), CollectionType = typeof(DayOfWeek[]))]
        public DayOfWeek[] DayOfWeek { get; set; } = [];

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

        protected override int Capacity => 2;
        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.CALENDAR];
        }
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _queryCol = this.GetPooledObject<QueryCol>();

            this.SetReturnables(_queryCol);
        }

        protected override void Begin(IServiceProvider provider)
        {
            if (this.HasParameter(this.Today))
            {
                this.StartDate = DateTime.Today;
                this.EndDate = this.StartDate.AddDays(1d).AddSeconds(-1d);
            }
            else if (this.HasParameter(this.Tomorrow))
            {
                this.StartDate = DateTime.Today.AddDays(1d);
                this.EndDate = this.StartDate.AddDays(1d).AddSeconds(-1d);
            }
        }

        protected override void Process(IServiceProvider provider)
        {
            this.GetParameters(this.StartDate, this.EndDate, this.IncludeUnmonitored, this.IncludeEpisodeFile, this.IncludeEpisodeImages, this.IncludeSeries);
            string url = this.Tag.GetUrl(_queryCol);

            var response = this.SendGetRequest<MetadataList<CalendarObject>>(url);
            if (response.IsError)
            {
                this.StopCmdlet(response.Error);
                return;
            }
            else if (this.HasParameter(x => x.DayOfWeek) && this.DayOfWeek.Length > 0)
            {
                this.FilterByDayOfWeek(response.Data, this.DayOfWeek);
            }

            this.WriteCollection(response.Data);
        }

        private void FilterByDayOfWeek(MetadataList<CalendarObject> list, DayOfWeek[] dows)
        {
            int removed = list.RemoveAll(predicate: item =>
            {
                return !dows.Contains(item.AirDateUtc.DayOfWeek);
            });

            this.WriteVerbose($"Filtered {removed} items from {nameof(list)}.");
        }
        private void GetParameters(DateTime start, DateTime end, bool unmonitored, bool includeEpisodeFile, bool includeEpisodeImages, bool includeSeries)
        {
            _queryCol.Add(
                [nameof(unmonitored), unmonitored],
                [nameof(includeEpisodeFile), includeEpisodeFile],
                [nameof(includeSeries), includeSeries],
                [nameof(includeEpisodeImages), includeEpisodeImages]
            );

            _queryCol.Add(START, start, Constants.CALENDAR_DT_FORMAT.Length, Constants.CALENDAR_DT_FORMAT);
            _queryCol.Add(END, end, Constants.CALENDAR_DT_FORMAT.Length, Constants.CALENDAR_DT_FORMAT);
        }
    }
}
