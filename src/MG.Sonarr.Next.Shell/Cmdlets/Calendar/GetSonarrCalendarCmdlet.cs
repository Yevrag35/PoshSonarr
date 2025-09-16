using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Calendar;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Output;
using MG.Sonarr.Next.Unions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Shell.Cmdlets.Calendar
{
    [Cmdlet(VerbsCommon.Get, "SonarrCalendar", DefaultParameterSetName = "None")]
    [OutputType(typeof(ICalendarOutput))]
    public sealed class GetSonarrCalendarCmdlet : SonarrMetadataCmdlet
    {
        const string START = "start";
        const string END = "end";
        static readonly TimeSpan WEEK_TIME_SPAN = TimeSpan.FromDays(8).Subtract(TimeSpan.FromSeconds(1));

        [Parameter(Position = 0)]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Position = 1), NotNull, DisallowNull]
        public DateTime? EndDate
        {
            get => field ??= this.StartDate.Add(WEEK_TIME_SPAN);
            set => field = value;
        }

        [Parameter]
        [DistinctValues(typeof(DayOfWeek))]
        public DayOfWeek[] DayOfWeek { get; set; } = [];

        [Parameter, ValidateNotNull, AllowEmptyCollection, ValidateIds(ValidateRangeKind.Positive, NullBehavior = InputNullBehavior.Ignore)]
        public Either<string, int>[] Tags { get; set; } = [];

        [Parameter(Mandatory = true, ParameterSetName = "ShowToday")]
        public SwitchParameter Today { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ShowTomorrow")]
        public SwitchParameter Tomorrow { get; set; }

        [Parameter]
        public SwitchParameter IncludeEpisodeFile { get; set; }

        [Parameter]
        public SwitchParameter IncludeEpisodeImages { get; set; }

        [Parameter]
        public SwitchParameter IncludeSeries { get; set; }

        [Parameter]
        public SwitchParameter IncludeUnmonitored { get; set; }

        private QueryCol _queryCol = null!;
        private SortedSet<int> _tagIds = null!;
        private WildcardSet _tagNames = null!;
        protected override int Capacity => 3;
        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.CALENDAR];
        }
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _queryCol = this.GetPooledObject<QueryCol>();
            _tagIds = this.GetPooledObject<SortedSet<int>>();
            _tagNames = this.GetPooledObject<WildcardSet>();

            this.SetReturnables(_queryCol, _tagIds, _tagNames);
        }

        protected override void Begin(IServiceProvider provider)
        {
            if (this.HasParameter(this.Today))
            {
                this.StartDate = DateTime.Today;
                this.EndDate = this.StartDate.AddDays(1).AddSeconds(-1);
            }
            else if (this.HasParameter(this.Tomorrow))
            {
                this.StartDate = DateTime.Today.AddDays(1);
                this.EndDate = this.StartDate.AddDays(1).AddSeconds(-1);
            }

            if (this.HasParameter(this.Tags))
            {
                this.Tags.SplitToSets(_tagIds, _tagNames, explicitlyCalledForString: false);
            }

            if (_tagNames.Count > 0)
            {
                MetadataTag tag = provider.GetMetadataTag(Meta.TAG);
                this.ProcessNames(_tagNames, _tagIds, tag);
            }
        }

        protected override void Process(IServiceProvider provider)
        {
            this.GetParameters(this.StartDate, this.EndDate.Value, this.IncludeUnmonitored, this.IncludeEpisodeFile, this.IncludeEpisodeImages, this.IncludeSeries);
            string url = this.Tag.GetUrl(_queryCol);

            var response = this.SendGetRequest<MetadataList<CalendarObject>>(url);
            if (response.IsError)
            {
                this.WriteConditionalError(response.Error);
                return;
            }
            else if (this.HasParameter(x => x.DayOfWeek) && this.DayOfWeek.Length > 0)
            {
                this.FilterByDayOfWeek(response.Value, this.DayOfWeek);
            }

            this.WriteCollection(response.Value);
        }

        private void FilterByDayOfWeek(MetadataList<CalendarObject> list, DayOfWeek[] dows)
        {
            int removed = 0;
            ReadOnlySpan<int> values = System.Runtime.CompilerServices.Unsafe.As<int[]>(dows).AsSpan();

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (values.IndexOf((int)list[i].AirDateUtc.DayOfWeek) == -1)
                {
                    list.RemoveAt(i);
                    removed++;
                }
            }

            this.WriteVerbose($"Filtered {removed} items from {nameof(list)}.");
        }
        private void GetParameters(DateTime start, DateTime end, bool unmonitored, bool includeEpisodeFile, bool includeEpisodeImages, bool includeSeries)
        {
            _queryCol.AddIfTrue(unmonitored);
            _queryCol.AddIfTrue(includeEpisodeFile);
            _queryCol.AddIfTrue(includeSeries);
            _queryCol.AddIfTrue(includeEpisodeImages);

            _queryCol.Add(START, start, Constants.CALENDAR_DT_FORMAT.Length, Constants.CALENDAR_DT_FORMAT);
            _queryCol.Add(END, end, Constants.CALENDAR_DT_FORMAT.Length, Constants.CALENDAR_DT_FORMAT);

            if (_tagIds.Count > 0)
            {
                _queryCol.Add("tags", string.Join(',', _tagIds));
            }
        }
        private void ProcessNames(WildcardSet names, SortedSet<int> tagIds, MetadataTag tag)
        {
            if (names.Count == 0)
            {
                return;
            }

            int prevCount = tagIds.Count;

            var tags = this.GetAll<TagObject>(tag.UrlBase);

            for (int i = tags.Count - 1; i >= 0; i--)
            {
                TagObject tagObj = tags[i];
                if (!tagIds.Contains(tagObj.Id) && names.IsAnyMatch(tagObj.Label))
                {
                    tagIds.Add(tagObj.Id);
                }
            }

            if (tagIds.Count == prevCount)
            {
                this.WriteWarning($"No tags were found matching the specified names - {string.Join(", ", names)}");
            }
        }
    }
}
