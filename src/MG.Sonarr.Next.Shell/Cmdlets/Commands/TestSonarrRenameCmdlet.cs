using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Renames;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Commands
{
    [Cmdlet(VerbsDiagnostic.Test, "SonarrRename")]
    public sealed class TestSonarrRenameCmdlet : SonarrApiCmdletBase
    {
        QueryParameterCollection _params = null!;

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "BySeriesInput")]
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

        protected override void Begin(IServiceProvider provider)
        {
            if (this.HasParameter(x => x.SeasonNumber))
            {
                _params = new(2)
                {
                    { nameof(this.SeasonNumber), this.SeasonNumber },
                };
            }
            else
            {
                _params = new();
            }
        }

        protected override void Process(IServiceProvider provider)
        {
            var parameter = QueryParameter.Create(nameof(this.SeriesId), this.SeriesId, LengthConstants.INT_MAX);
            _params.AddOrUpdate(parameter);

            string url = GetUrl(_params);
            var response = this.SendGetRequest<MetadataList<RenameObject>>(url);
            _ = this.TryWriteObject(in response);
        }

        private static string GetUrl(QueryParameterCollection parameters)
        {
            ReadOnlySpan<char> endpoint = Constants.RENAME.AsSpan();
            Span<char> span = stackalloc char[endpoint.Length + parameters.MaxLength + 1];

            int position = 0;
            endpoint.CopyToSlice(span, ref position);
            span[position++] = '?';

            _ = parameters.TryFormat(span.Slice(position), out int written, default, Statics.DefaultProvider);
            return new string(span.Slice(0, position + written));
        }
    }
}
