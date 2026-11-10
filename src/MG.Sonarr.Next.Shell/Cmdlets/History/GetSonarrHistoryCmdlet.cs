using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.History;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using System.ComponentModel;

namespace MG.Sonarr.Next.Shell.Cmdlets.History;

/// <summary>
/// Represents the <c>Get-SonarrHistory</c> cmdlet, which gets the records of what Sonarr has done with episodes, such
/// as grabbing, importing, deleting, or renaming them.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet writes each record to the pipeline as a <see cref="HistoryObject"/>, and sends its requests once its
/// input ends. When a request fails, the cmdlet writes its error.
/// </para>
/// <para>
/// The default parameter set gets one page of the history, sorted by <c>-SortKey</c> in <c>-SortDirection</c>, and
/// <c>-EpisodeId</c> and <c>-DownloadId</c> narrow it to one episode or one download. <c>-Since</c> gets every record
/// since a date and time. <c>-SeriesId</c>, or a series piped to the cmdlet, gets every record of each series, with a
/// request for each one.
/// </para>
/// <para>
/// In every parameter set, <c>-EventType</c> limits the records to one kind of event, and <c>-IncludeEpisode</c> and
/// <c>-IncludeSeries</c> add each record's episode or series to it.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrHistory", DefaultParameterSetName = BY_PAGING)]
[MetadataCanPipe(Tag = Meta.Series)]
public sealed class GetSonarrHistoryCmdlet : SonarrMetadataCmdlet
{
	private const string BY_PAGING = "ByPaging";
	private const string BY_SERIES_ID = "ByExplicitSeriesId";
	private const string BY_SERIES_PIPE = "BySeriesPipelineInput";
	private const string SINCE_DATE = "SinceDate";

	private SortedSet<int> _ids = null!;
	private QueryCol _parameters = null!;

	#region BY_PAGING PARAMETER SET

	/// <summary>
	/// Gets or sets the number of the page to get.
	/// </summary>
	/// <remarks>
	/// The cmdlet sends the page number only when the parameter is given, and Get-Help shows a default of 1.
	/// </remarks>
	/// <value>The page's number, starting at 1. The default is 0, which sends no page number.</value>
	[Parameter(ParameterSetName = BY_PAGING)]
	[ValidateRange(ValidateRangeKind.Positive)]
	[PSDefaultValue(Value = 1)]
	public int PageNumber { get; set; }

	/// <summary>
	/// Gets or sets the number of records on each page.
	/// </summary>
	/// <remarks>
	/// The cmdlet sends the page size only when the parameter is given, and Get-Help shows a default of 10.
	/// </remarks>
	/// <value>The page's size, greater than zero. The default is 0, which sends no page size.</value>
	[Parameter(ParameterSetName = BY_PAGING)]
	[ValidateRange(ValidateRangeKind.Positive)]
	[PSDefaultValue(Value = 10)]
	public int PageSize { get; set; }

	/// <summary>
	/// Gets or sets the direction that the page's records are sorted in.
	/// </summary>
	/// <remarks>
	/// The cmdlet sends the direction only when the parameter is given, and Get-Help shows a default of
	/// <c>Descending</c>.
	/// </remarks>
	/// <value>The sort direction. The default is <see cref="ListSortDirection.Ascending"/>, which isn't sent.</value>
	[Parameter(ParameterSetName = BY_PAGING)]
	[PSDefaultValue(Value = ListSortDirection.Descending)]
	public ListSortDirection SortDirection { get; set; }

	/// <summary>
	/// Gets or sets the name of the field that the page's records are sorted by.
	/// </summary>
	/// <remarks>
	/// Tab completion offers the fields of a history record, such as <c>Date</c>, <c>EventType</c>, and
	/// <c>SeriesId</c>, and any other name is sent as it's given.
	/// </remarks>
	/// <value>The field's name. The default is <c>Id</c>.</value>
	[Parameter(ParameterSetName = BY_PAGING)]
	[ArgumentCompletions("Data", "Date", "EpisodeId", "EventType", "Id", "Language", "Quality", "SeriesId", "SourceTitle")]
	[ValidateNotNullOrEmpty]
	[PSDefaultValue(Value = "Id")]
	public string SortKey { get; set; } = "Id";

	/// <summary>
	/// Gets or sets the ID that a download client gave the download whose records the cmdlet gets.
	/// </summary>
	/// <value>The download's ID, or <see langword="null"/> for the records of every download.</value>
	[Parameter(ParameterSetName = BY_PAGING)]
	public string DownloadId { get; set; } = null!;

	/// <summary>
	/// Gets or sets the ID of the episode whose records the cmdlet gets.
	/// </summary>
	/// <value>The episode's ID, greater than zero. The default is 0, for the records of every episode.</value>
	[Parameter(ParameterSetName = BY_PAGING)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int EpisodeId { get; set; }

	#endregion

	#region OTHER PARAMETER SETS

	/// <summary>
	/// Gets or sets the date and time that the cmdlet gets every record since.
	/// </summary>
	/// <remarks>
	/// The cmdlet writes a warning when the value is later than the current time. A UTC value, such as
	/// <c>[datetime]::UtcNow</c> gives, is compared as UTC, and any other value as local time.
	/// </remarks>
	/// <value>The date and time that the records start at.</value>
	[Parameter(Mandatory = true, ParameterSetName = SINCE_DATE)]
	public DateTime Since { get; set; }

	/// <summary>
	/// Gets or sets the series whose records the cmdlet gets.
	/// </summary>
	/// <remarks>
	/// The parameter takes the series that <c>Get-SonarrSeries</c> writes from the pipeline, and each piped series
	/// arrives in an array of its own. Each series' ID has to be greater than zero.
	/// </remarks>
	/// <value>The piped series. The default is an empty array.</value>
	[Parameter(Mandatory = true, ParameterSetName = BY_SERIES_PIPE, ValueFromPipeline = true)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public SeriesObject[] Series { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs of the series whose records the cmdlet gets.
	/// </summary>
	/// <value>The series' IDs, each greater than zero. The default is an empty array.</value>
	[Parameter(Mandatory = true, ParameterSetName = BY_SERIES_ID)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] SeriesId { get; set; } = [];

	#endregion

	/// <summary>
	/// Gets or sets the kind of event whose records the cmdlet gets.
	/// </summary>
	/// <remarks>
	/// The cmdlet sends the event type's number, and only when the parameter is given. PowerShell binds a member's name,
	/// such as <c>Grabbed</c> or <c>EpisodeFileDeleted</c>, ignoring case, or its number, and tab completion offers the
	/// names.
	/// </remarks>
	/// <value>
	/// The event type. The default is <see cref="EpisodeHistoryEventType.Unknown"/>, which isn't sent.
	/// </value>
	[Parameter]
	public EpisodeHistoryEventType EventType { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether each record includes its episode.
	/// </summary>
	/// <value>The switch, which is present to include the episodes.</value>
	[Parameter]
	public SwitchParameter IncludeEpisode { get; set; }

	/// <summary>
	/// Gets or sets a value that indicates whether each record includes its series.
	/// </summary>
	/// <value>The switch, which is present to include the series.</value>
	[Parameter]
	public SwitchParameter IncludeSeries { get; set; }

	/// <summary>
	/// Gets the metadata tag of the history endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The history's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.History];
	}

	/// <summary>
	/// Runs the base class's setup for the scope, then creates the set of series IDs and the query that the cmdlet
	/// fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
		_parameters = new(10);
	}
	/// <summary>
	/// Adds the IDs from <c>-SeriesId</c> to the series that the cmdlet gets records for, and adds to the query the
	/// switches that are present, the paging and sorting parameters in the default parameter set, and the event type.
	/// </summary>
	/// <remarks>
	/// The cmdlet writes a warning when <c>-Since</c> is later than the current time of the scope's
	/// <see cref="TimeProvider"/>.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.SeriesId);

		if (this.IncludeEpisode.IsPresent)
		{
			_parameters.Add([nameof(this.IncludeEpisode), this.IncludeEpisode.ToBool()]);
		}

		if (this.IncludeSeries.IsPresent)
		{
			_parameters.Add([nameof(this.IncludeSeries), this.IncludeSeries.ToBool()]);
		}

		var timeProvider = provider.GetRequiredService<TimeProvider>();
		if (this.HasParameter(Since) && IsInFuture(this.Since, timeProvider))
		{
			this.WriteWarning($"The specified parameter '{nameof(this.Since)}' is set to a time in the future. This could give unpredictable results.");
		}

		if (this.ParameterSetName == BY_PAGING)
		{
			this.SetPagingParams();
		}

		if (this.HasParameter(this.EventType))
		{
			_parameters.Add(nameof(this.EventType), (int)this.EventType);
		}
	}
	/// <summary>
	/// Adds the IDs of the piped series to the series that the cmdlet gets records for.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Process(IServiceProvider provider)
	{
		if (this.Series.Length > 0)
		{
			_ids.UnionWith(this.Series.Select(x => x.Id));
		}
	}

	/// <summary>
	/// Sends the request, or the requests, that the parameter set calls for, and writes the records to the pipeline.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		switch (this.ParameterSetName)
		{
			case BY_PAGING:
				this.SendPagingQuery(_parameters);
				break;

			case BY_SERIES_ID:
			case BY_SERIES_PIPE:
				this.SendSeriesQuery(provider, _parameters, _ids);
				break;

			case SINCE_DATE:
				this.SendSinceQuery(provider, _parameters, this.Since);
				break;

			default:
				return;
		}
	}

	#region PAGING FUNCTIONALITY

	/// <summary>
	/// Adds <c>-EpisodeId</c> and <c>-DownloadId</c> to the specified query when they're given, gets one page of the
	/// history, and writes the page's records to the pipeline.
	/// </summary>
	/// <remarks>
	/// When the request fails, the method writes its error, whatever it is.
	/// </remarks>
	/// <param name="parameters">The query, which already holds the paging and sorting parameters.</param>
	private void SendPagingQuery(QueryCol parameters)
	{
		if (this.HasParameter(this.EpisodeId))
		{
			parameters.Add(nameof(this.EpisodeId), this.EpisodeId);
		}

		if (this.HasParameter(this.DownloadId))
		{
			parameters.Add(nameof(this.DownloadId), this.DownloadId);
		}

		string url = this.Tag.GetUrl(parameters);
		var response = this.SendGetRequest<RecordResult<HistoryObject>>(url);
		_ = this.TryWriteObject(response, writeConditionally: false, enumerateCollection: true, x => x.Records);
	}

	/// <summary>
	/// Adds the page number, the page size, and the sort direction to the query when they're given, and the sort key
	/// always.
	/// </summary>
	private void SetPagingParams()
	{
		if (this.HasParameter(this.PageNumber))
		{
			_parameters.Add(PagingConstants.PageNumber, this.PageNumber);
		}

		if (this.HasParameter(this.PageSize))
		{
			_parameters.Add(PagingConstants.PageSize, this.PageSize);
		}

		if (this.HasParameter(this.SortDirection))
		{
			_parameters.Add(PagingConstants.SortDirection, this.SortDirection, this.SortDirection.Length);
		}

		_parameters.Add(PagingConstants.SortKey, this.SortKey);
	}

	#endregion

	#region SERIES FUNCTIONALITY

	/// <summary>
	/// Gets every record of each of the specified series, with a request for each series to the series history
	/// endpoint, and writes the records to the pipeline.
	/// </summary>
	/// <remarks>
	/// Each request carries the series' ID as its <c>seriesId</c> query parameter, which replaces the previous
	/// series' ID. When a request fails, the method writes its error, whatever it is, and goes on to the next series.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider, which gives the endpoint's metadata tag.</param>
	/// <param name="parameters">The query, which the method adds each series' ID to.</param>
	/// <param name="ids">The IDs of the series, in the order that the requests go out.</param>
	private void SendSeriesQuery(IServiceProvider provider, QueryCol parameters, SortedSet<int> ids)
	{
		var tag = provider.GetMetadataTag(Meta.SeriesHistory);

		if (ids.Count == 0)
		{
			return;
		}

		foreach (int id in ids)
		{
			FormattableQueryField field = new("seriesId", id, LengthConstants.INT_MAX);
			_parameters.AddOrUpdate(field);

			string url = tag.GetUrl(parameters);

			var response = this.SendGetRequest<MetadataList<HistoryObject>>(url);
			_ = this.TryWriteObject(response, writeConditionally: false, enumerateCollection: true);
		}
	}

	#endregion

	#region SINCE FUNCTIONALITY

	/// <summary>
	/// Determines whether the specified date and time is later than the current time of the specified time provider.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The method compares moments in time, and the value's <see cref="DateTime.Kind"/> decides which moment it stands
	/// for: a <see cref="DateTimeKind.Utc"/> value is UTC, a <see cref="DateTimeKind.Local"/> value is in the
	/// computer's time zone, as <see cref="DateTime.ToUniversalTime"/> reads it, and a
	/// <see cref="DateTimeKind.Unspecified"/> value is in the provider's <see cref="TimeProvider.LocalTimeZone"/>.
	/// </para>
	/// <para>
	/// The value's UTC time is computed in ticks rather than with
	/// <see cref="TimeZoneInfo.ConvertTimeToUtc(DateTime, TimeZoneInfo)"/>, which throws for a local time that daylight
	/// saving time skips. Such a time takes the zone's standard offset, and a value near <see cref="DateTime.MinValue"/>
	/// or <see cref="DateTime.MaxValue"/> doesn't throw either.
	/// </para>
	/// </remarks>
	/// <param name="value">The date and time to compare.</param>
	/// <param name="timeProvider">The time provider that gives the current time and the local time zone.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="value"/> is later than the current time; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	private static bool IsInFuture(DateTime value, TimeProvider timeProvider)
	{
		long utcTicks = value.Kind switch
		{
			DateTimeKind.Utc => value.Ticks,
			DateTimeKind.Local => value.ToUniversalTime().Ticks,
			_ => value.Ticks - timeProvider.LocalTimeZone.GetUtcOffset(value).Ticks,
		};

		return utcTicks > timeProvider.GetUtcNow().UtcTicks;
	}

	/// <summary>
	/// Gets every record since the specified date and time from the history-since endpoint, and writes the records to
	/// the pipeline.
	/// </summary>
	/// <remarks>
	/// The date goes in the query as <c>date</c>, formatted with <see cref="Constants.CalendarDateTimeFormat"/>. When
	/// the request fails, the method writes its error, whatever it is.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider, which gives the endpoint's metadata tag.</param>
	/// <param name="parameters">The query, which the method adds the date to.</param>
	/// <param name="date">The date and time that the records start at.</param>
	private void SendSinceQuery(IServiceProvider provider, QueryCol parameters, DateTime date)
	{
		var tag = provider.GetMetadataTag(Meta.HistorySince);
		parameters.Add(nameof(date), date, Constants.CalendarDateTimeFormat.Length, Constants.CalendarDateTimeFormat);

		string url = tag.GetUrl(parameters);
		var response = this.SendGetRequest<MetadataList<HistoryObject>>(url);
		_ = this.TryWriteObject(response, writeConditionally: false, enumerateCollection: true);
	}

	#endregion
}

/// <summary>
/// Specifies the kinds of event that Sonarr records in an episode's history, with the numbers that Sonarr gives them.
/// </summary>
public enum EpisodeHistoryEventType
{
	/// <summary>An event of no known kind.</summary>
	Unknown = 0,
	/// <summary>A release of the episode was grabbed and sent to a download client.</summary>
	Grabbed = 1,
	/// <summary>An episode file was imported from the series' folder.</summary>
	SeriesFolderImported = 2,
	/// <summary>An episode file was imported from a download client's folder.</summary>
	DownloadFolderImported = 3,
	/// <summary>A download of the episode failed.</summary>
	DownloadFailed = 4,
	/// <summary>The episode's file was deleted.</summary>
	EpisodeFileDeleted = 5,
	/// <summary>The episode's file was renamed.</summary>
	EpisodeFileRenamed = 6,
	/// <summary>A download of the episode was ignored.</summary>
	DownloadIgnored = 7,
}
