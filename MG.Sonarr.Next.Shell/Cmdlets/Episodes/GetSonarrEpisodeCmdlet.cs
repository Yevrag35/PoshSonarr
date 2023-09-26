using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Episodes;
using MG.Sonarr.Next.Services.Models.Series;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Episodes
{
    [Cmdlet(VerbsCommon.Get, "SonarrEpisode", DefaultParameterSetName = "ByEpisodeId")]
    public sealed class GetSonarrEpisodeCmdlet : SonarrApiCmdletBase
    {
        SortedSet<int> EpIds { get; set; } = null!;
        SortedSet<int> SeriesIds { get; set; } = null!;
        MetadataTag Tag { get; }

        public GetSonarrEpisodeCmdlet()
            : base()
        {
            this.Tag = this.Services.GetRequiredService<MetadataResolver>()[Meta.EPISODE];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByEpisodeId")]
        public int[] Id
        {
            get => Array.Empty<int>();
            set
            {
                this.EpIds ??= new();
                this.EpIds.UnionWith(value);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "BySeriesId")]
        public int[] SeriesId
        {
            get => Array.Empty<int>();
            set
            {
                this.SeriesIds ??= new();
                this.SeriesIds.UnionWith(value);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByEpisodeInput", DontShow = true)]
        public IEpisodePipeable[] EpisodeInput
        {
            get => Array.Empty<IEpisodePipeable>();
            set
            {
                this.EpIds ??= new();
                this.EpIds.UnionWith(value.Select(x => x.EpisodeId));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "BySeriesInput", DontShow = true)]
        public IEpisodeBySeriesPipeable[] SeriesInput
        {
            get => Array.Empty<IEpisodeBySeriesPipeable>();
            set
            {
                this.SeriesIds ??= new();
                this.SeriesIds.UnionWith(value.Select(x => x.SeriesId));
            }
        }

        [Parameter(Mandatory = false, Position = 1, ParameterSetName = "BySeriesId")]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "BySeriesInput")]
        [Alias("SeasonEpId")]
        public SeasonEpisodeId EpisodeIdentifier { get; set; }

        protected override void Begin()
        {
            if (this.HasParameter(x => x.EpisodeIdentifier) && this.EpisodeIdentifier.IsEmpty)
            {
                this.WriteError(new ArgumentException("Episode identifiers should be either be in \"S<seasonNumber>E<episodeNumber>\" format or a number.")
                    .ToRecord(ErrorCategory.InvalidArgument, this.EpisodeIdentifier));
            }
        }

        protected override void End()
        {
            if (this.EpIds is null && this.SeriesIds is null)
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

        private IEnumerable<T> GetEpisodesBySeries<T>(IReadOnlySet<int> seriesId) where T : PSObject, IJsonMetadataTaggable
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
    }
}
