using MG.Http.Urls.Queries;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.ManualImports;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Shell.Extensions;
using System.Net;

//using CanBeNullAttribute = System.Management.Automation.AllowNullAttribute;
using IOPath = System.IO.Path;

namespace MG.Sonarr.Next.Shell.Cmdlets.ManualImports
{
    [Cmdlet(VerbsCommon.Get, "SonarrManualImport", DefaultParameterSetName = BY_FOLDER)]
    public sealed class GetSonarrManualImportCmdlet : SonarrApiCmdletBase
    {
        const string BY_FOLDER = "ByFolderPath";
        const string BY_DOWNLOAD = "ByDownloadId";
        const string BY_SERIES = "ByExplicitSeriesId";
        const string BY_SERIES_PIPE = "BySeriesPipelineInput";
        const string FILTER_EXISTING = "filterExistingFiles";
        const string FOLDER = "folder";
        const int PARAM_CAPACITY = 2;
        QueryParameterCollection _parameters = null!;

        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = BY_FOLDER)]
        [Alias("PSPath")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; } = string.Empty;

        [Parameter(Mandatory = true, ParameterSetName = BY_DOWNLOAD)]
        [ValidateNotNullOrEmpty]
        public string DownloadId { get; set; } = string.Empty;

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = BY_SERIES)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int SeriesId { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = BY_SERIES_PIPE, ValueFromPipeline = true)]
        [ValidateNotNull]
        public SeriesObject Series
        {
            get => null!;
            set => this.SeriesId = value?.Id ?? 0;
        }

        [Parameter(Position = 1, ParameterSetName = BY_SERIES)]
        [Parameter(Position = 0, ParameterSetName = BY_SERIES_PIPE)]
        [ValidateRange(ValidateRangeKind.NonNegative)]
        public int SeasonNumber { get; set; }

        [Parameter]
        public SwitchParameter IncludeExistingFiles { get; set; }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _parameters = new QueryParameterCollection(PARAM_CAPACITY);
        }
        protected override void Begin(IServiceProvider provider)
        {
            switch (this.ParameterSetName)
            {
                case BY_DOWNLOAD:
                    _parameters.Add(nameof(this.DownloadId), this.DownloadId);
                    break;

                case BY_SERIES:
                case BY_SERIES_PIPE:
                    SetSeriesParameters(
                        this.SeriesId,
                        this.SeasonNumber,
                        wantsSeason: this.HasParameter(x => x.SeasonNumber),
                        _parameters);

                    break;

                default:
                    break;
            }

            SetFilterParameter(this.IncludeExistingFiles, _parameters);
        }
        protected override void Process(IServiceProvider provider)
        {
            var tag = provider.GetMetadataTag(Meta.MANUAL_IMPORT);

            if (this.ParameterSetName == BY_FOLDER)
            {
                string formattedPath = this.ResolveAndFormatPath(this.Path);
                _parameters.AddOrUpdate(QueryParameter.Create(FOLDER, formattedPath));
            }

            string url = tag.GetUrl(_parameters);
            var response = this.SendGetRequest<MetadataList<ManualImportObject>>(url);
            _ = this.TryWriteObject(in response);
        }

        private string ResolveAndFormatPath(string path)
        {
            path = path.TrimEnd('/', '\\');
            if (!IOPath.IsPathFullyQualified(path))
            {
                path = this.GetResolvedPath(path);
            }

            this.WriteVerbose($"Original path -> {path}");
            path = WebUtility.UrlEncode(path);
            this.WriteVerbose($"URL-formatted path -> {path}");

            return path;
        }
        private static void SetFilterParameter(bool includeExisting, QueryParameterCollection parameters)
        {
            parameters.Add(FILTER_EXISTING, !includeExisting);
        }
        private static void SetSeriesParameters(int seriesId, int seasonNumber, bool wantsSeason, QueryParameterCollection parameters)
        {
            parameters.Add(nameof(seriesId), seriesId);
            if (wantsSeason)
            {
                parameters.Add(nameof(seasonNumber), seasonNumber);
            }
        }
    }
}

