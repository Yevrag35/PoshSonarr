using MG.Sonarr.Next.Services;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Episodes;
using MG.Sonarr.Next.Services.Models.WantedMissing;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.WantedMissing
{
    [Cmdlet(VerbsCommon.Get, "SonarrWantedMissing", DefaultParameterSetName = "ByPage")]
    public sealed class GetSonarrWantedMissingCmdlet : SonarrApiCmdletBase
    {
        QueryParameterCollection QueryCol { get; }

        public GetSonarrWantedMissingCmdlet()
            : base()
        {
            this.QueryCol = new(3);
        }

        [Parameter(Mandatory = true, ParameterSetName = "AllRecords")]
        public SwitchParameter All { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, ParameterSetName = "ByPage")]
        [Alias("Page")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int PageNumber
        {
            get => default;
            set => this.QueryCol.Add("page", value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = false, ParameterSetName = "ByPage")]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int PageSize
        {
            get => default;
            set => this.QueryCol.Add("pageSize", value);
        }

        protected override void Process()
        {
            if (this.All.ToBool())
            {
                IEnumerable<EpisodeObject> records = this.SendAllRecords();
                this.WriteCollection(records);
                return;
            }

            string url = GetUrl(this.QueryCol);
            if (this.GetWantedMissing(url, out WantedMissingObject? result))
            {
                this.WriteCollection(result.Records);
            }
        }

        private bool GetWantedMissing(string url, [NotNullWhen(true)] out WantedMissingObject? result)
        {
            result = null;
            var response = this.SendGetRequest<WantedMissingObject>(url);
            if (response.IsError)
            {
                this.WriteError(response.Error);
                return false;
            }

            result = response.Data;
            return true;
        }

        private static string GetUrl(QueryParameterCollection parameters)
        {
            if (parameters.Count <= 0)
            {
                return Constants.WANTEDMISSING;
            }

            Span<char> span = stackalloc char[Constants.WANTEDMISSING.Length + 1 + parameters.MaxLength];
            Constants.WANTEDMISSING.CopyTo(span);
            int position = Constants.WANTEDMISSING.Length;

            span[position++] = '?';
            _ = parameters.TryFormat(span.Slice(position), out int written, default, Statics.DefaultProvider);
            return new string(span.Slice(0, position + written));
        }

        private IEnumerable<EpisodeObject> SendAllRecords()
        {
            this.PageNumber = 1;
            this.PageSize = 1;

            string url = GetUrl(this.QueryCol);
            if (!this.GetWantedMissing(url, out WantedMissingObject? result))
            {
                return Enumerable.Empty<EpisodeObject>();
            }

            this.QueryCol.Remove(nameof(this.PageSize));
            this.PageSize = result.TotalRecords;

            url = GetUrl(this.QueryCol);
            _ = this.GetWantedMissing(url, out result);

            return result?.Records ?? Enumerable.Empty<EpisodeObject>();
        }
    }
}
