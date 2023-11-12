using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

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
        QueryCol _params = null!;
        Dictionary<int, IEpisodeBySeriesPipeable> _seriesIds = null!;
        protected override int Capacity => CAPACITY;

        [Parameter(Mandatory = true, ParameterSetName = BY_EP_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] Id { get; set; } = [];

        [Parameter(Mandatory = true, ParameterSetName = BY_SERIES_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int[] SeriesId { get; set; } = [];

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = BY_EP_INPUT)]
        [ValidateIds(ValidateRangeKind.Positive, typeof(IEpisodePipeable))]
        public IEpisodePipeable[] EpisodeInput { get; set; } = [];

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = BY_SERIES_INPUT)]
        [ValidateIds(ValidateRangeKind.Positive, typeof(IEpisodeBySeriesPipeable))]
        public IEpisodeBySeriesPipeable[] SeriesInput { get; set; } = [];

        [Parameter(Mandatory = false, Position = 1, ParameterSetName = BY_SERIES_ID)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = BY_SERIES_INPUT)]
        [Alias("SeasonEpId")]
        public SeasonEpisodeId[]? EpisodeIdentifier { get; set; }

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.EPISODE];
        }
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _epIds = this.GetPooledObject<SortedSet<int>>();
            _seriesIds = this.GetPooledObject<Dictionary<int, IEpisodeBySeriesPipeable>>();
            _params = this.GetPooledObject<QueryCol>();

            this.SetReturnables(_epIds, _seriesIds, _params);
        }
        [SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
        protected override void Begin(IServiceProvider provider)
        {
            if (this.HasNotNullParameter(this.EpisodeIdentifier) && this.EpisodeIdentifier.Length > 0 && !this.EpisodeIdentifier.AreAllValid(out ErrorRecord? error))
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
        [SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
        protected override void End(IServiceProvider provider)
        {
            if (this.InvokeCommand.HasErrors || (_epIds.IsNullOrEmpty() && _seriesIds.IsNullOrEmpty()))
            {
                return;
            }

            IEnumerable<EpisodeObject> episodes = this.ParameterSetNameIsLike("ByEpisode*")
                ? this.GetEpisodesById<EpisodeObject>(_epIds!)
                : this.GetEpisodesBySeries(_seriesIds);

            if (this.HasNotNullParameter(EpisodeIdentifier) && this.EpisodeIdentifier.Length > 0)
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
                    yield return response.Value;
                }
            }
        }
        private List<EpisodeObject> GetEpisodesBySeries(Dictionary<int, IEpisodeBySeriesPipeable> series)
        {
            List<EpisodeObject> list = new(series.Count);
            foreach (int id in series.Keys)
            {
                _params.Add(Constants.SERIES_ID_LOWERCASE, id);
                string url = this.Tag.GetUrl(_params);
                
                var response = this.SendGetRequest<MetadataList<EpisodeObject>>(url);
                if (response.IsError)
                {
                    this.WriteConditionalError(response.Error);
                    continue;
                }

                foreach (EpisodeObject obj in response.Value)
                {
                    if (_seriesIds.TryGetValue(obj.SeriesId, out IEpisodeBySeriesPipeable? s))
                    {
                        obj.SetSeries(s);
                    }

                    list.Add(obj);
                }

                _params.Clear();
            }

            return list;
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
