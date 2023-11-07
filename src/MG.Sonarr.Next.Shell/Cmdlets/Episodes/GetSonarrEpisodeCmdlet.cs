using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Collections.Pools;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.Episodes
{
    [Cmdlet(VerbsCommon.Get, "SonarrEpisode", DefaultParameterSetName = BY_EP_ID)]
    [MetadataCanPipe(Tag = Meta.CALENDAR)]
    [MetadataCanPipe(Tag = Meta.SERIES)]
    public sealed class GetSonarrEpisodeCmdlet : SonarrMetadataCmdlet
    {
        const string BY_EP_ID = "ByEpisodeId";
        const string BY_EP_INPUT = "ByEpisodeInput";
        const string BY_SERIES_ID = "BySeriesId";
        const string BY_SERIES_INPUT = "BySeriesInput";
        const int CAPACITY = 3;

        SortedSet<int> _epIds = null!;
        QueryParameterCollection _params = null!;
        Dictionary<int, IEpisodeBySeriesPipeable> _seriesIds = null!;
        protected override int Capacity => CAPACITY;

        [Parameter(Mandatory = true, ParameterSetName = BY_EP_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = Array.Empty<int>();

        [Parameter(Mandatory = true, ParameterSetName = BY_SERIES_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] SeriesId { get; set; } = Array.Empty<int>();

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = BY_EP_INPUT)]
        [ValidateIds(ValidateRangeKind.Positive, typeof(IEpisodePipeable))]
        public IEpisodePipeable[] EpisodeInput { get; set; } = Array.Empty<IEpisodePipeable>();

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = BY_SERIES_INPUT)]
        [ValidateIds(ValidateRangeKind.Positive, typeof(IEpisodeBySeriesPipeable))]
        public IEpisodeBySeriesPipeable[] SeriesInput { get; set; } = Array.Empty<IEpisodeBySeriesPipeable>();

        [Parameter(Mandatory = false, Position = 1, ParameterSetName = BY_SERIES_ID)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = BY_SERIES_INPUT)]
        [Alias("SeasonEpId")]
        public SeasonEpisodeId[] EpisodeIdentifier { get; set; } = Array.Empty<SeasonEpisodeId>();

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.EPISODE];
        }
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _epIds = this.GetPooledObject<SortedSet<int>>();
            _seriesIds = this.GetPooledObject<Dictionary<int, IEpisodeBySeriesPipeable>>();
            _params = this.GetPooledObject<QueryParameterCollection>();
            var span = this.GetReturnables();
            span[0] = _epIds;
            span[1] = _seriesIds;
            span[2] = _params;
        }

        protected override void Begin(IServiceProvider provider)
        {
            if (this.HasParameter(x => x.EpisodeIdentifier) && !this.EpisodeIdentifier.AreAllValid(out ErrorRecord? error))
            {
                this.WriteError(error);
            }
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
            foreach (IEpisodeBySeriesPipeable series in input)
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
                string url = this.Tag.GetUrlForId(id);
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
            foreach (int id in series.Keys)
            {
                _params.Add(Constants.SERIES_ID, id);
                string url = this.Tag.GetUrl(_params);
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

                _params.Clear();
            }
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

            int? IPipeable<IEpisodeBySeriesPipeable>.GetId()
            {
                return null;
            }

            internal static readonly EmptySeries Default = default;
        }
    }
}
