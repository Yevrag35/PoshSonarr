using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.History;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Services.Time;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Completers;
using MG.Sonarr.Next.Shell.Extensions;
using System.ComponentModel;

namespace MG.Sonarr.Next.Shell.Cmdlets.History
{
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
        PagingParameter _paging = null!;
        QueryParameterCollection _parameters = null!;

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
        public SeriesObject[] Series { get; set; } = Array.Empty<SeriesObject>();

        [Parameter(Mandatory = true, ParameterSetName = BY_SERIES_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] SeriesId { get; set; } = Array.Empty<int>();

        #endregion

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
            _paging = this.GetPooledObject<PagingParameter>();
            this.Returnables[0] = _paging;

            _ids = this.GetPooledObject<SortedSet<int>>();
            this.Returnables[1] = _ids;

            _parameters = new(4);
        }

        protected override void Begin(IServiceProvider provider)
        {
            _ids.UnionWith(this.SeriesId);

            if (this.HasParameter(x => x.IncludeEpisode, onlyIfPresent: true))
            {
                _parameters.Add(QueryParameter.Create(nameof(this.IncludeEpisode), this.IncludeEpisode.ToBool()));
            }

            if (this.HasParameter(x => x.IncludeSeries, onlyIfPresent: true))
            {
                _parameters.Add(QueryParameter.Create(nameof(this.IncludeSeries), this.IncludeSeries.ToBool()));
            }

            var clock = provider.GetRequiredService<IClock>();
            if (this.HasParameter(x => x.Since) && this.Since > clock.Now.DateTime)
            {
                this.WriteWarning($"The specified parameter '{nameof(this.Since)}' is set to a time in the future. This could give unpredicatable results.");
            }

            if (this.ParameterSetName == BY_PAGING)
            {
                this.SetPagingParams();
            }
            
            if (this.HasParameter(x => x.EventType) && _eventType > -1)
            {
                _parameters.Add(nameof(this.EventType), _eventType, LengthConstants.INT_MAX);
            }
        }

        protected override void Process(IServiceProvider provider)
        {
            if (this.HasParameter(x => x.Series))
            {
                _ids.UnionWith(this.Series.Select(x => x.Id));
            }

            //var response = this.SendGetRequest<RecordResult<HistoryObject>>(Constants.HISTORY + "/since?date=" + DateTime.Today.AddDays(-7d).ToString(Constants.CALENDAR_DT_FORMAT));
            //_ = this.TryWriteObject(in response);
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

        private void SendPagingQuery(QueryParameterCollection parameters)
        {
            if (this.HasParameter(x => x.EpisodeId))
            {
                parameters.Add(nameof(this.EpisodeId), this.EpisodeId);
            }
            
            if (this.HasParameter(x => x.DownloadId))
            {
                parameters.Add(nameof(this.DownloadId), this.DownloadId);
            }

            string url = this.Tag.GetUrl(parameters);
            var response = this.SendGetRequest<RecordResult<HistoryObject>>(url);
            _ = this.TryWriteObject(in response, writeConditionally: false, enumerateCollection: true, x => x.Records);
        }

        private void SetPagingParams()
        {
            if (this.HasParameter(x => x.PageNumber))
            {
                _paging.Page = this.PageNumber;
            }

            if (this.HasParameter(x => x.PageSize))
            {
                _paging.PageSize = this.PageSize;
            }

            if (this.HasParameter(x => x.SortDirection))
            {
                _paging.SortDirection = this.SortDirection;
            }

            _paging.SortKey = this.SortKey;

            _parameters.AddOrUpdate(_paging);
        }

        #endregion

        #region SERIES FUNCTIONALITY

        private void SendSeriesQuery(IServiceProvider provider, QueryParameterCollection parameters, SortedSet<int> ids)
        {
            var tag = provider.GetMetadataTag(Meta.SERIES_HISTORY);

            if (ids.Count <= 0)
            {
                return;
            }

            foreach (int id in ids)
            {
                parameters.AddOrUpdate(QueryParameter.Create(nameof(this.SeriesId), id, LengthConstants.INT_MAX));
                string url = tag.GetUrl(parameters);

                var response = this.SendGetRequest<MetadataList<HistoryObject>>(url);
                _ = this.TryWriteObject(in response, writeConditionally: false, enumerateCollection: true);
            }
        }

        #endregion

        #region SINCE FUNCTIONALITY

        private void SendSinceQuery(IServiceProvider provider, QueryParameterCollection parameters, DateTime date)
        {
            var tag = provider.GetMetadataTag(Meta.HISTORY_SINCE);
            parameters.Add(nameof(date), date, Constants.CALENDAR_DT_FORMAT.Length, Constants.CALENDAR_DT_FORMAT);

            string url = tag.GetUrl(parameters);
            var response = this.SendGetRequest<MetadataList<HistoryObject>>(url);
            _ = this.TryWriteObject(in response, writeConditionally: false, enumerateCollection: true);
        }

        #endregion
    }
}
