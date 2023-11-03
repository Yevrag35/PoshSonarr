using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Releases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Releases
{
    [Cmdlet(VerbsCommon.Search, "SonarrRelease", DefaultParameterSetName = "ByEpisodeId")]
    [MetadataCanPipe(Tag = Meta.EPISODE)]
    [MetadataCanPipe(Tag = Meta.SERIES)]
    public sealed class SearchSonarrReleaseCmdlet : SonarrApiCmdletBase
    {
        QueryParameterCollection QueryParams { get; set; } = null!;
        MetadataTag Tag { get; set; } = null!;

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByEpisodeId")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int EpisodeId { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByEpisodeInput")]
        public IReleasePipeableByEpisode Episode
        {
            get => null!;
            set => this.EpisodeId = value?.EpisodeId ?? 0;
        }

        [Parameter(Mandatory = true, ParameterSetName = "BySeriesId")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int SeriesId { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "BySeriesInput")]
        public IReleasePipeableBySeries Series
        {
            get => null!;
            set => this.SeriesId = value?.SeriesId ?? 0;
        }

        [Parameter(Mandatory = true, ParameterSetName = "BySeriesId")]
        [Parameter(Mandatory = true, ParameterSetName = "BySeriesInput")]
        [Alias("Season")]
        [ValidateRange(ValidateRangeKind.NonNegative)]
        public int SeasonNumber { get; set; }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Tag = provider.GetRequiredService<IMetadataResolver>()[Meta.RELEASE];
            this.QueryParams = new();
        }

        protected override void Process(IServiceProvider provider)
        {
            this.QueryParams.Clear();

            if (this.EpisodeId <= 0 && this.SeriesId <= 0)
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

            this.WriteCollection(response.Data);
        }

        private string GetUrl()
        {
            if (this.ParameterSetNameIsLike("ByEpisode*"))
            {
                this.QueryParams.Add(this, LengthConstants.INT_MAX, x => x.EpisodeId);
            }
            else
            {
                this.QueryParams.Add(nameof(this.SeriesId), this.SeriesId);
                this.QueryParams.Add(nameof(this.SeasonNumber), this.SeasonNumber);
            }
            
            return this.Tag.GetUrl(this.QueryParams);
        }
    }
}
