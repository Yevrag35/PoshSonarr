using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Episodes
{
    [Cmdlet(VerbsCommon.Get, "SonarrEpisode", DefaultParameterSetName = BY_EP_ID)]
    [MetadataCanPipe(Tag = Meta.CALENDAR)]
    [MetadataCanPipe(Tag = Meta.SERIES)]
    public sealed class GetSonarrEpisodeCmdlet : SonarrApiCmdletBase
    {
        const string BY_EP_ID = "ByEpisodeId";
        const string BY_EP_INPUT = "ByEpisodeInput";
        const string BY_SERIES_ID = "BySeriesId";
        const string BY_SERIES_INPUT = "BySeriesInput";
        bool _disposed;

        SortedSet<int> _epIds = null!;
        //SortedSet<int> _seriesIds = null!;
        Dictionary<int, IEpisodeBySeriesPipeable> _seriesIds = null!;
        MetadataTag _tag = null!;

        [Parameter(Mandatory = true, ParameterSetName = BY_EP_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

        [Parameter(Mandatory = true, ParameterSetName = BY_SERIES_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] SeriesId { get; set; } = Array.Empty<int>();

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = BY_EP_INPUT)]
        [ValidateId(ValidateRangeKind.Positive)]
        public IEpisodePipeable[] EpisodeInput { get; set; } = Array.Empty<IEpisodePipeable>();

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = BY_SERIES_INPUT)]
        [ValidateId(ValidateRangeKind.Positive)]
        public IEpisodeBySeriesPipeable[] SeriesInput { get; set; } = Array.Empty<IEpisodeBySeriesPipeable>();

        [Parameter(Mandatory = false, Position = 1, ParameterSetName = BY_SERIES_ID)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = BY_SERIES_INPUT)]
        [Alias("SeasonEpId")]
        public SeasonEpisodeId[] EpisodeIdentifier { get; set; } = Array.Empty<SeasonEpisodeId>();

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _tag = provider.GetRequiredService<IMetadataResolver>()[Meta.EPISODE];
            var pool = provider.GetRequiredService<IObjectPool<SortedSet<int>>>();
            _epIds = pool.Get();
            //_seriesIds = pool.Get();
            _seriesIds = new(1);
        }

        protected override void Begin(IServiceProvider provider)
        {
            if (this.HasParameter(x => x.EpisodeIdentifier) && !this.EpisodeIdentifier.AreAllValid(out ErrorRecord? error))
            {
                this.WriteError(error);
            }
            //if (this.HasParameter(x => x.EpisodeIdentifier) && this.EpisodeIdentifier.IsEmpty)
            //{
            //    this.WriteError(new ArgumentException("Episode identifiers should be either be in \"S<seasonNumber>E<episodeNumber>\" format or a number.")
            //        .ToRecord(ErrorCategory.InvalidArgument, this.EpisodeIdentifier));
            //}
        }

        protected override void Process(IServiceProvider provider)
        {
            switch (this.ParameterSetName)
            {
                case BY_EP_ID:
                    _epIds.UnionWith(this.Id);
                    break;

                case BY_SERIES_ID:
                    AddSeriesIds(this.SeriesId, _seriesIds);
                    break;

                case BY_EP_INPUT:
                    _epIds.UnionWith(this.EpisodeInput.Select(x => x.EpisodeId));
                    break;

                case BY_SERIES_INPUT:
                    AddSeries(this.SeriesInput, _seriesIds);
                    break;
            }
        }

        protected override void End(IServiceProvider provider)
        {
            if (this.InvokeCommand.HasErrors || (_epIds.IsNullOrEmpty() && _seriesIds.IsNullOrEmpty()))
            {
                return;
            }

            IEnumerable<EpisodeObject> episodes = this.ParameterSetNameIsLike("ByEpisode*")
                ? this.GetEpisodesById<EpisodeObject>(_epIds!)
                : this.GetEpisodesBySeries(_seriesIds);

            if (this.HasParameter(x => x.EpisodeIdentifier))
            {
                episodes = this.EpisodeIdentifier.FilterEpisodes(episodes);
            }

            this.WriteCollection(episodes);
        }

        private static void AddSeries(IEpisodeBySeriesPipeable[] input, IDictionary<int, IEpisodeBySeriesPipeable> dict)
        {
            foreach (var series in input)
            {
                _ = dict.TryAdd(series.SeriesId, series);
            }
        }
        private static void AddSeriesIds(int[] ids, IDictionary<int, IEpisodeBySeriesPipeable> dict)
        {
            foreach (int id in ids)
            {
                _ = dict.TryAdd(id, EmptySeries.Default);
            }
        }

        private IEnumerable<T> GetEpisodesById<T>(IEnumerable<int> episodeIds) where T : PSObject, IJsonMetadataTaggable
        {
            foreach (int id in episodeIds)
            {
                string url = _tag.GetUrlForId(id);
                var response = this.SendGetRequest<T>(url);
                if (response.IsError)
                {
                    this.WriteConditionalError(response.Error);
                }
                else
                {
                    yield return response.Data;
                }
            }
        }
        private IEnumerable<EpisodeObject> GetEpisodesBySeries(IReadOnlyDictionary<int, IEpisodeBySeriesPipeable> series)
        {
            QueryParameterCollection queryCol = new();
            foreach (int id in series.Keys)
            {
                queryCol.Add(Constants.SERIES_ID, id);
                string url = _tag.GetUrl(queryCol);
                var response = this.SendGetRequest<MetadataList<EpisodeObject>>(url);
                if (response.IsError)
                {
                    this.WriteConditionalError(response.Error);
                    continue;
                }

                foreach (EpisodeObject obj in response.Data)
                {
                    if (_seriesIds.TryGetValue(obj.SeriesId, out IEpisodeBySeriesPipeable? s))
                    {
                        obj.SetSeries(s);
                    }

                    yield return obj;
                }

                queryCol.Clear();
            }
        }

        protected override void Dispose(bool disposing, IServiceScopeFactory? factory)
        {
            if (disposing && !_disposed)
            {
                if (factory is not null)
                {
                    using var scope = factory.CreateScope();
                    var pool = scope.ServiceProvider.GetService<IObjectPool<SortedSet<int>>>();
                    pool?.Return(_epIds);
                }

                _epIds = null!;
                _seriesIds = null!;
                _disposed = true;
            }

            base.Dispose(disposing, factory);
        }

        private readonly struct EmptySeries : IEpisodeBySeriesPipeable
        {
            public int SeriesId => 0;
            public string Title => string.Empty;

            public MetadataTag MetadataTag => MetadataTag.Empty;

            public void SetTag(IMetadataResolver resolver)
            {
                return;
            }

            internal static readonly EmptySeries Default = default;
        }
    }
}
