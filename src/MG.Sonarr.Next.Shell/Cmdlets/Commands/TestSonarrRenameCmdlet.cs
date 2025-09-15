using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Renames;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Commands
{
    [Cmdlet(VerbsDiagnostic.Test, "SonarrRename")]
    [MetadataCanPipe(Tag = Meta.CALENDAR)]
    [MetadataCanPipe(Tag = Meta.EPISODE)]
    [MetadataCanPipe(Tag = Meta.EPISODE_FILE)]
    [MetadataCanPipe(Tag = Meta.SERIES)]
    public sealed class TestSonarrRenameCmdlet : SonarrApiCmdletBase
    {
        QueryCol _params = null!;

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "BySeriesInput")]
        [ValidateId(ValidateRangeKind.Positive, typeof(ISeriesPipeable))]
        public ISeriesPipeable InputObject
        {
            get => null!;
            set => this.SeriesId = value.SeriesId;
        }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByExplicitSeriesId")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int SeriesId { get; set; }

        [Parameter(Position = 0, ParameterSetName = "BySeriesInput")]
        [Parameter(Position = 1, ParameterSetName = "ByExplicitSeriesId")]
        [Alias("Season")]
        [ValidateRange(ValidateRangeKind.NonNegative)]
        public int SeasonNumber { get; set; }

        protected override int Capacity => 1;

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            _params = this.GetPooledObject<QueryCol>();
            this.SetReturnables(_params);
        }

        protected override void Begin(IServiceProvider provider)
        {
            if (this.HasParameter(this.SeasonNumber))
            {
                _params.Add("seasonNumber", this.SeasonNumber);
            }
        }

        protected override void Process(IServiceProvider provider)
        {
            int index = _params.Count;
            _params.Add("seriesId", this.SeriesId);

            string url = GetUrl(_params);
            var response = this.SendGetRequest<MetadataList<RenameObject>>(url);
            _ = this.TryWriteObject(response);
            _params.RemoveAt(index);
        }

        private static string GetUrl(QueryCol parameters)
        {
            ReadOnlySpan<char> endpoint = Constants.RENAME.AsSpan();
            Span<char> span = stackalloc char[endpoint.Length + parameters.MaxLength + 1];

            int position = 0;
            endpoint.CopyToSlice(span, ref position);

            _ = parameters.TryFormat(span.Slice(position), out int written);
            return new string(span.Slice(0, position + written));
        }
    }
}
