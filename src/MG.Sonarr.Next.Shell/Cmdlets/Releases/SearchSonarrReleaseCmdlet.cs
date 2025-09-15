using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Releases;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Releases
{
    [Cmdlet(VerbsCommon.Search, "SonarrRelease", DefaultParameterSetName = ByEpisodeId)]
    [MetadataCanPipe(Tag = Meta.EPISODE)]
    [MetadataCanPipe(Tag = Meta.SERIES)]
    public sealed class SearchSonarrReleaseCmdlet : SonarrApiCmdletBase
    {
        private const string ByEpisodeId = "ByEpisodeId";
        private const string ByEpisodeInput = "ByEpisodeInput";
        private const string BySeriesId = "BySeriesId";
        private const string BySeriesInput = "BySeriesInput";
        private static readonly Wildcard _byEpisodeWildcard = Wildcard
            .ParseAs(WildcardMatchType.StartsWith, ByEpisodeId.AsSpan(0, ByEpisodeId.Length - 2));

        QueryCol _queryParams = null!;
        MetadataTag Tag { get; set; } = null!;

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = ByEpisodeId)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int EpisodeId { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ByEpisodeInput)]
        [ValidateId(ValidateRangeKind.Positive, typeof(IReleasePipeableByEpisode))]
        public IReleasePipeableByEpisode Episode
        {
            get => null!;
            set => this.EpisodeId = value?.EpisodeId ?? 0;
        }

        [Parameter(Mandatory = true, ParameterSetName = BySeriesId)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int SeriesId { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = BySeriesInput)]
        [ValidateId(ValidateRangeKind.Positive, typeof(IReleasePipeableBySeries))]
        public IReleasePipeableBySeries Series
        {
            get => null!;
            set => this.SeriesId = value?.SeriesId ?? 0;
        }

        [Parameter(Mandatory = true, ParameterSetName = BySeriesId)]
        [Parameter(Mandatory = true, ParameterSetName = BySeriesInput)]
        [Alias("Season")]
        [ValidateRange(ValidateRangeKind.NonNegative)]
        public int SeasonNumber { get; set; }
        protected override int Capacity => 1;

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Tag = provider.GetRequiredService<IMetadataResolver>()[Meta.RELEASE];
            _queryParams = this.GetPooledObject<QueryCol>();
            this.SetReturnables(_queryParams);
        }

        protected override void Process(IServiceProvider provider)
        {
            _queryParams.Clear();

            if (this.EpisodeId == 0 && this.SeriesId == 0)
            {
                return;
            }

            string url = this.GetUrl();

            var response = this.SendGetRequest<MetadataList<ReleaseObject>>(url);
            if (response.IsError)
            {
                this.WriteError(response.Error);
                return;
            }

            this.WriteCollection(response.Value);
        }

        private string GetUrl()
        {
            if (this.ParameterSetNameIsLike(_byEpisodeWildcard))
            {
                _queryParams.Add(Constants.EPISODE_ID, this.EpisodeId);
            }
            else
            {
                _queryParams.Add(Constants.SERIES_ID_LOWERCASE, this.SeriesId);
                _queryParams.Add(Constants.SEASON_NUMBER, this.SeasonNumber);
            }
            
            return this.Tag.GetUrl(_queryParams);
        }
    }
}
