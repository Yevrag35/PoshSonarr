using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Episodes
{
    [Cmdlet(VerbsCommon.Get, "SonarrEpisode", DefaultParameterSetName = BY_EP_ID)]
    public sealed class GetSonarrEpisodeCmdlet : SonarrApiCmdletBase
    {
        const string BY_EP_ID = "ByEpisodeId";
        const string BY_EP_INPUT = "ByEpisodeInput";
        const string BY_SERIES_ID = "BySeriesId";
        const string BY_SERIES_INPUT = "BySeriesInput";
        bool _disposed;

        SortedSet<int> EpIds { get; set; } = null!;
        SortedSet<int> SeriesIds { get; set; } = null!;
        MetadataTag Tag { get; set; } = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = BY_EP_ID)]
        public int[] Id { get; set; } = Array.Empty<int>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = BY_SERIES_ID)]
        public int[] SeriesId { get; set; } = Array.Empty<int>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = BY_EP_INPUT, DontShow = true)]
        public IEpisodePipeable[] EpisodeInput { get; set; } = Array.Empty<IEpisodePipeable>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = BY_SERIES_INPUT, DontShow = true)]
        public IEpisodeBySeriesPipeable[] SeriesInput { get; set; } = Array.Empty<IEpisodeBySeriesPipeable>();

        [Parameter(Mandatory = false, Position = 1, ParameterSetName = BY_SERIES_ID)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = BY_SERIES_INPUT)]
        [Alias("SeasonEpId")]
        public SeasonEpisodeId EpisodeIdentifier { get; set; }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Tag = provider.GetRequiredService<IMetadataResolver>()[Meta.EPISODE];
            var pool = provider.GetRequiredService<IObjectPool<SortedSet<int>>>();
            this.EpIds = pool.Get();
            this.SeriesIds = pool.Get();
        }

        protected override void Begin(IServiceProvider provider)
        {
            if (this.HasParameter(x => x.EpisodeIdentifier) && this.EpisodeIdentifier.IsEmpty)
            {
                this.WriteError(new ArgumentException("Episode identifiers should be either be in \"S<seasonNumber>E<episodeNumber>\" format or a number.")
                    .ToRecord(ErrorCategory.InvalidArgument, this.EpisodeIdentifier));
            }
        }

        protected override void Process(IServiceProvider provider)
        {
            switch (this.ParameterSetName)
            {
                case BY_EP_ID:
                    this.EpIds.UnionWith(this.Id);
                    break;

                case BY_SERIES_ID:
                    this.SeriesIds.UnionWith(this.SeriesId);
                    break;

                case BY_EP_INPUT:
                    this.EpIds.UnionWith(this.EpisodeInput.Select(x => x.EpisodeId));
                    break;

                case BY_SERIES_INPUT:
                    this.SeriesIds.UnionWith(this.SeriesInput.Select(x => x.SeriesId));
                    break;
            }
        }

        protected override void End(IServiceProvider provider)
        {
            if (this.InvokeCommand.HasErrors || (this.EpIds.IsNullOrEmpty() && this.SeriesIds.IsNullOrEmpty()))
            {
                return;
            }

            IEnumerable<EpisodeObject> episodes = this.ParameterSetNameIsLike("ByEpisode*")
                ? this.GetEpisodesById<EpisodeObject>(this.EpIds!)
                : this.GetEpisodesBySeries<EpisodeObject>(this.SeriesIds);

            if (this.HasParameter(x => x.EpisodeIdentifier) && !this.EpisodeIdentifier.IsEmpty)
            {
                if (this.EpisodeIdentifier.IsAbsolute)
                {
                    episodes = episodes.Where(x =>
                        x.HasAbsolute
                        &&
                        x.AbsoluteEpisodeNumber == this.EpisodeIdentifier.Episode);
                }
                else
                {
                    episodes = episodes.Where(x => x.EpisodeNumber == this.EpisodeIdentifier.Episode
                                                   &&
                                                   x.SeasonNumber == this.EpisodeIdentifier.Season);
                }
            }

            this.WriteCollection(episodes);
        }

        private IEnumerable<T> GetEpisodesById<T>(IReadOnlySet<int> episodeIds) where T : PSObject, IJsonMetadataTaggable
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

        private IEnumerable<T> GetEpisodesBySeries<T>(IReadOnlySet<int> seriesId)
            where T : PSObject, IComparable<T>, IJsonMetadataTaggable
        {
            QueryParameterCollection queryCol = new();
            foreach (int id in seriesId)
            {
                queryCol.Add(Constants.SERIES_ID, id);
                string url = this.Tag.GetUrl(queryCol);
                var response = this.SendGetRequest<MetadataList<T>>(url);
                if (response.IsError)
                {
                    this.WriteConditionalError(response.Error);
                    continue;
                }

                foreach (T obj in response.Data)
                {
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
                    pool?.Return(this.EpIds);
                    pool?.Return(this.SeriesIds);
                }

                this.EpIds = null!;
                this.SeriesIds = null!;
                _disposed = true;
            }

            base.Dispose(disposing, factory);
        }
    }
}
