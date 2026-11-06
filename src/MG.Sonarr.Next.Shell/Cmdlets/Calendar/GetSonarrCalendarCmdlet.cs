using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Calendar;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Output;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Calendar;

/// <summary>
/// Represents the <c>Get-SonarrCalendar</c> cmdlet, which gets the calendar entries of the episodes that air in a range
/// of dates.
/// </summary>
/// <remarks>
/// <para>
/// The range runs from <c>-StartDate</c> to <c>-EndDate</c>, in local time, and <c>-Today</c> or <c>-Tomorrow</c> sets
/// it to one whole day instead. Sonarr returns the entries in the range, and the cmdlet writes each one to the pipeline
/// as a <see cref="CalendarObject"/>.
/// </para>
/// <para>
/// <c>-Tags</c> limits the entries to the series that carry one of the tags, and <c>-DayOfWeek</c> to the episodes that
/// air on one of the days. <c>-IncludeEpisodeFile</c>, <c>-IncludeEpisodeImages</c>, and <c>-IncludeSeries</c> add each
/// episode's file, images, or series to its entry, and <c>-IncludeUnmonitored</c> adds the entries of unmonitored
/// episodes.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrCalendar", DefaultParameterSetName = "None")]
[OutputType(typeof(ICalendarOutput))]
public sealed class GetSonarrCalendarCmdlet : SonarrMetadataCmdlet
{
	private const string START = "start";
	private const string END = "end";
	private static readonly TimeSpan WEEK_TIME_SPAN = TimeSpan.FromDays(8).Subtract(TimeSpan.FromSeconds(1));

	/// <summary>
	/// Gets or sets the start of the range of dates.
	/// </summary>
	/// <remarks>
	/// When the parameter isn't given, the cmdlet sets the value when it begins, from the scope's
	/// <see cref="TimeProvider"/>, since a property initializer runs before the cmdlet has a scope. <c>-Today</c> and
	/// <c>-Tomorrow</c> replace the value when the cmdlet begins.
	/// </remarks>
	/// <value>
	/// The local date and time that the range starts at. The default is the local date and time when the cmdlet begins.
	/// </value>
	[Parameter(Position = 0)]
	public DateTime StartDate { get; set; }

	/// <summary>
	/// Gets or sets the end of the range of dates.
	/// </summary>
	/// <remarks>
	/// Until a value is set, the first read computes the default from <see cref="StartDate"/> and keeps it. PowerShell's
	/// binder never reads a parameter that takes no pipeline input, so the first read comes after the cmdlet has set
	/// <see cref="StartDate"/>'s default when it begins. <c>-Today</c> and <c>-Tomorrow</c> replace the value when the
	/// cmdlet begins.
	/// </remarks>
	/// <value>
	/// The local date and time that the range ends at, which a read never gives as <see langword="null"/>. The default is
	/// a second before 8 days after <see cref="StartDate"/>.
	/// </value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Position = 1), NotNull, DisallowNull]
	public DateTime? EndDate
	{
		get => field ??= this.StartDate.Add(WEEK_TIME_SPAN);
		set => field = value;
	}

	/// <summary>
	/// Gets or sets the days of the week that the entries air on.
	/// </summary>
	/// <remarks>
	/// Sonarr returns the entries of every day, and the cmdlet removes those of the other days, as their air dates fall
	/// in local time. A day given more than once binds once.
	/// </remarks>
	/// <value>The days whose entries the cmdlet writes, or <see langword="null"/> or empty for every day.</value>
	[Parameter]
	[DistinctValues(typeof(DayOfWeek))]
	public DayOfWeek[]? DayOfWeek { get; set; }

	/// <summary>
	/// Gets or sets the IDs and names of the tags whose series' entries the cmdlet gets.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A number, or text that reads as a whole number, is a tag's ID, which has to be greater than zero. Any other text
	/// is a name, which can hold wildcards and matches a tag's label ignoring case.
	/// </para>
	/// <para>
	/// Sonarr returns the entries of the series that carry any of the tags. When no tag matches the names, the cmdlet
	/// writes a warning, and when no ID was given either, it gets no entries.
	/// </para>
	/// </remarks>
	/// <value>The IDs and names of the tags. The default is an empty array, for the entries of every series.</value>
	[Parameter, ValidateNotNull, AllowEmptyCollection, ValidateIds(ValidateRangeKind.Positive, NullBehavior = InputNullBehavior.Ignore)]
	public Either<string, int>[] Tags { get; set; } = [];

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet gets today's entries.
	/// </summary>
	/// <remarks>
	/// The range runs from midnight today to a second before midnight tomorrow, in local time, in place of the one that
	/// <c>-StartDate</c> and <c>-EndDate</c> give. The switch can't be combined with <c>-Tomorrow</c>.
	/// </remarks>
	/// <value>The switch, which is present to get today's entries.</value>
	[Parameter(Mandatory = true, ParameterSetName = "ShowToday")]
	public SwitchParameter Today { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet gets tomorrow's entries.
	/// </summary>
	/// <remarks>
	/// The range runs from midnight tomorrow to a second before midnight the day after, in local time, in place of the
	/// one that <c>-StartDate</c> and <c>-EndDate</c> give. The switch can't be combined with <c>-Today</c>.
	/// </remarks>
	/// <value>The switch, which is present to get tomorrow's entries.</value>
	[Parameter(Mandatory = true, ParameterSetName = "ShowTomorrow")]
	public SwitchParameter Tomorrow { get; set; }

	// Possibly coming in v4
	//[Parameter]
	//public IntOrString[] Tag { get; set; } = Array.Empty<IntOrString>();

	/// <summary>
	/// Gets or sets a value that indicates whether each entry includes its episode's file.
	/// </summary>
	/// <value>The switch, which is present to include the episode files.</value>
	[Parameter]
	public SwitchParameter IncludeEpisodeFile { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether each entry includes its episode's images.
	/// </summary>
	/// <value>The switch, which is present to include the episode images.</value>
	[Parameter]
	public SwitchParameter IncludeEpisodeImages { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether each entry includes its episode's series.
	/// </summary>
	/// <value>The switch, which is present to include the series.</value>
	[Parameter]
	public SwitchParameter IncludeSeries { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet also gets the entries of unmonitored episodes.
	/// </summary>
	/// <value>The switch, which is present to include unmonitored episodes.</value>
	[Parameter]
	public SwitchParameter IncludeUnmonitored { get; set; }

	private QueryCol _queryCol = null!;
	private SortedSet<int> _tagIds = null!;
	private WildcardSet _tagNames = null!;
	/// <summary>
	/// Gets the metadata tag of the calendar endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The calendar's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.Calendar];
	}
	/// <summary>
	/// Runs the base class's setup for the scope, then creates the query and the sets of tag IDs and names that the
	/// cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_queryCol = new(10);
		_tagIds = [];
		_tagNames = new();
	}

	/// <summary>
	/// Sets the range of dates for <c>-Today</c> or <c>-Tomorrow</c>, or the start of the range when <c>-StartDate</c>
	/// isn't given, and finds the IDs of the tags whose labels match the names in <c>-Tags</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The current date and time come from the scope's <see cref="TimeProvider"/>, in its local time zone.
	/// </para>
	/// <para>
	/// Matching the names gets every tag with one request, and when that request fails, the cmdlet stops with its error.
	/// </para>
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		DateTimeOffset now = provider.GetRequiredService<TimeProvider>().GetLocalNow();

		if (this.Today)
		{
			this.StartDate = now.Date;
			this.EndDate = this.StartDate.AddDays(1).AddSeconds(-1);
		}
		else if (this.Tomorrow)
		{
			this.StartDate = now.Date.AddDays(1);
			this.EndDate = this.StartDate.AddDays(1).AddSeconds(-1);
		}
		else if (!this.HasParameter(this.StartDate))
		{
			this.StartDate = now.DateTime;
		}

		if (this.HasParameter(this.Tags))
		{
			this.Tags.SplitToSets(_tagIds, _tagNames, explicitlyCalledForString: false);
		}

		if (_tagNames.Count > 0)
		{
			MetadataTag tag = provider.GetMetadataTag(Meta.Tag);
			this.ProcessNames(_tagNames, _tagIds, tag);
		}
	}

	/// <summary>
	/// Gets the calendar entries in the range of dates and writes them to the pipeline, without those that air on a day
	/// that <c>-DayOfWeek</c> leaves out.
	/// </summary>
	/// <remarks>
	/// When the request fails, the cmdlet writes its error, unless the error is ignorable, and writes no entries. When
	/// no tag matched the names in <c>-Tags</c> and no ID was given, the cmdlet sends no request.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming")]
	protected override void Process(IServiceProvider provider)
	{
		// Sonarr returns the entries whose series has any of the tags sent, and every entry when none is sent.
		// When no name matched a tag, an empty name included, and no ID was given, the filter matches nothing.
		if (_tagNames.Count > 0 && _tagIds.Count == 0)
		{
			return;
		}

		this.GetParameters(this.StartDate, this.EndDate.Value, this.IncludeUnmonitored, this.IncludeEpisodeFile, this.IncludeEpisodeImages, this.IncludeSeries);
		string url = this.Tag.GetUrl(_queryCol);

		var response = this.SendGetRequest<MetadataList<CalendarObject>>(url);
		if (response.IsError)
		{
			this.WriteConditionalError(response.Error);
			return;
		}
		else if (this.HasNotNullParameter(DayOfWeek) && this.DayOfWeek.Length > 0)
		{
			this.FilterByDayOfWeek(response.Value, this.DayOfWeek);
		}

		this.WriteCollection(response.Value);
	}

	/// <summary>
	/// Removes from the specified list the entries that don't air on one of the specified days, and writes to the
	/// verbose stream how many it removed.
	/// </summary>
	/// <remarks>
	/// An entry's day is the one that its air date falls on in local time, since the dates that the cmdlet takes are
	/// local. The days are compared as their <see cref="int"/> values, read from the array without copying it.
	/// </remarks>
	/// <param name="list">The entries that Sonarr returned, which the method removes entries from.</param>
	/// <param name="dows">The days whose entries the list keeps.</param>
	private void FilterByDayOfWeek(MetadataList<CalendarObject> list, DayOfWeek[] dows)
	{
		int removed = 0;
		ReadOnlySpan<int> values = System.Runtime.CompilerServices.Unsafe.As<int[]>(dows).AsSpan();

		for (int i = list.Count - 1; i >= 0; i--)
		{
			// The dates that the cmdlet takes are local, so an entry's day is the one that it airs on in local time.
			if (values.IndexOf((int)list[i].AirDateUtc.ToLocalTime().DayOfWeek) == -1)
			{
				list.RemoveAt(i);
				removed++;
			}
		}

		this.WriteVerbose($"Filtered {removed} items from {nameof(list)}.");
	}
	/// <summary>
	/// Adds the request's query parameters to the cmdlet's query: one for each switch that is present, the range of
	/// dates, and the IDs of the tags.
	/// </summary>
	/// <remarks>
	/// A switch's parameter takes the name of the argument passed for it, through the caller argument expression of
	/// <see cref="QueryCol.AddIfTrue(bool, string)"/>, so the arguments' names are the query's keys. The dates are
	/// formatted with <see cref="Constants.CalendarDateTimeFormat"/>.
	/// </remarks>
	/// <param name="start">The start of the range.</param>
	/// <param name="end">The end of the range.</param>
	/// <param name="unmonitored"><see langword="true"/> to include unmonitored episodes; otherwise, <see langword="false"/>.</param>
	/// <param name="includeEpisodeFile"><see langword="true"/> to include the episode files; otherwise, <see langword="false"/>.</param>
	/// <param name="includeEpisodeImages"><see langword="true"/> to include the episode images; otherwise, <see langword="false"/>.</param>
	/// <param name="includeSeries"><see langword="true"/> to include the series; otherwise, <see langword="false"/>.</param>
	private void GetParameters(DateTime start, DateTime end, bool unmonitored, bool includeEpisodeFile, bool includeEpisodeImages, bool includeSeries)
	{
		_queryCol.AddIfTrue(unmonitored);
		_queryCol.AddIfTrue(includeEpisodeFile);
		_queryCol.AddIfTrue(includeSeries);
		_queryCol.AddIfTrue(includeEpisodeImages);

		_queryCol.Add(START, start, Constants.CalendarDateTimeFormat.Length, Constants.CalendarDateTimeFormat);
		_queryCol.Add(END, end, Constants.CalendarDateTimeFormat.Length, Constants.CalendarDateTimeFormat);

		if (_tagIds.Count > 0)
		{
			_queryCol.Add("tags", string.Join(',', _tagIds));
		}
	}
	/// <summary>
	/// Adds to the specified set the IDs of the tags whose labels match one of the specified names, and writes a warning
	/// when no tag matches.
	/// </summary>
	/// <remarks>
	/// The method gets every tag with one request, and when that request fails, the cmdlet stops with its error.
	/// </remarks>
	/// <param name="names">The names, which can hold wildcards, that the tags' labels are matched against.</param>
	/// <param name="tagIds">The tag IDs that the cmdlet sends, which the method adds the matching tags' IDs to.</param>
	/// <param name="tag">The metadata tag of the tag endpoint.</param>
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
			// Quoted, so that an empty or white-space name shows in the list.
			this.WriteWarning($"No tags were found matching the specified names - '{string.Join("', '", names)}'");
		}
	}
}
