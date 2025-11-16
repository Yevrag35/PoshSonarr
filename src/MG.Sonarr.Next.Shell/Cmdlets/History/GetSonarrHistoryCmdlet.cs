using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.History;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Services.Time;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Completers;
using MG.Sonarr.Next.Shell.Extensions;
using System.ComponentModel;

namespace MG.Sonarr.Next.Shell.Cmdlets.History;

[Cmdlet(VerbsCommon.Get, "SonarrHistory", DefaultParameterSetName = BY_PAGING)]
[MetadataCanPipe(Tag = Meta.SERIES)]
public sealed class GetSonarrHistoryCmdlet : SonarrMetadataCmdlet
{
	const string BY_PAGING = "ByPaging";
	const string BY_SERIES_ID = "ByExplicitSeriesId";
	const string BY_SERIES_PIPE = "BySeriesPipelineInput";
	const string SINCE_DATE = "SinceDate";

	const int CAPACITY = 2;

	int _eventType = -1;
	SortedSet<int> _ids = null!;
	QueryCol _parameters = null!;

	protected override int Capacity => CAPACITY;

	#region BY_PAGING PARAMETER SET

	[Parameter(ParameterSetName = BY_PAGING)]
	[ValidateRange(ValidateRangeKind.Positive)]
	[PSDefaultValue(Value = 1)]
	public int PageNumber { get; set; }

	[Parameter(ParameterSetName = BY_PAGING)]
	[ValidateRange(ValidateRangeKind.Positive)]
	[PSDefaultValue(Value = 10)]
	public int PageSize { get; set; }

	[Parameter(ParameterSetName = BY_PAGING)]
	[PSDefaultValue(Value = ListSortDirection.Descending)]
	public ListSortDirection SortDirection { get; set; }

	[Parameter(ParameterSetName = BY_PAGING)]
	[ArgumentCompletions("Data", "Date", "EpisodeId", "EventType", "Id", "Lanugage", "Quality", "SeriesId", "SourceTitle")]
	[ValidateNotNullOrEmpty]
	[PSDefaultValue(Value = "Id")]
	public string SortKey { get; set; } = "Id";

	[Parameter(ParameterSetName = BY_PAGING)]
	public string DownloadId { get; set; } = null!;

	[Parameter(ParameterSetName = BY_PAGING)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int EpisodeId { get; set; }

	#endregion

	#region OTHER PARAMETER SETS

	[Parameter(Mandatory = true, ParameterSetName = SINCE_DATE)]
	public DateTime Since { get; set; }

	[Parameter(Mandatory = true, ParameterSetName = BY_SERIES_PIPE, ValueFromPipeline = true)]
	[ValidateIds(ValidateRangeKind.Positive)]
	public SeriesObject[] Series { get; set; } = [];

	[Parameter(Mandatory = true, ParameterSetName = BY_SERIES_ID)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] SeriesId { get; set; } = [];

	#endregion

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter]
	[ArgumentCompleter(typeof(EventTypeCompleter))]
	[ValidateNotNullOrEmpty]
	public string EventType
	{
		get => string.Empty;
		set => _eventType = EventTypeCompleter.GetNumberFromEventType(value);
	}

	[Parameter]
	public SwitchParameter IncludeEpisode { get; set; }

	[Parameter]
	public SwitchParameter IncludeSeries { get; set; }

	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.HISTORY];
	}

	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = this.GetPooledObject<SortedSet<int>>();
		_parameters = this.GetPooledObject<QueryCol>();

		this.SetReturnables(_ids, _parameters);
	}
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

		var clock = provider.GetRequiredService<IClock>();
		if (this.HasParameter(Since) && this.Since > clock.Now.DateTime)
		{
			this.WriteWarning($"The specified parameter '{nameof(this.Since)}' is set to a time in the future. This could give unpredicatable results.");
		}

		if (this.ParameterSetName == BY_PAGING)
		{
			this.SetPagingParams();
		}

		if (this.HasParameter(EventType) && _eventType > -1)
		{
			_parameters.Add(nameof(this.EventType), _eventType, LengthConstants.INT_MAX);
		}
	}
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Process(IServiceProvider provider)
	{
		if (this.Series.Length > 0)
		{
			_ids.UnionWith(this.Series.Select(x => x.Id));
		}
	}

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
			_parameters.Add(PagingConstants.SortDirection, this.SortDirection, this.SortDirection.GetLength());
		}

		_parameters.Add(PagingConstants.SortKey, this.SortKey);
	}

	#endregion

	#region SERIES FUNCTIONALITY

	private void SendSeriesQuery(IServiceProvider provider, QueryCol parameters, SortedSet<int> ids)
	{
		var tag = provider.GetMetadataTag(Meta.SERIES_HISTORY);

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

	private void SendSinceQuery(IServiceProvider provider, QueryCol parameters, DateTime date)
	{
		var tag = provider.GetMetadataTag(Meta.HISTORY_SINCE);
		parameters.Add(nameof(date), date, Constants.CALENDAR_DT_FORMAT.Length, Constants.CALENDAR_DT_FORMAT);

		string url = tag.GetUrl(parameters);
		var response = this.SendGetRequest<MetadataList<HistoryObject>>(url);
		_ = this.TryWriteObject(response, writeConditionally: false, enumerateCollection: true);
	}

	#endregion
}
