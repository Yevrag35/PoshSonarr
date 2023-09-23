using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Series;
using MG.Sonarr.Next.Shell.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Add, "SonarrSeries", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
        DefaultParameterSetName = "RootFolderPath")]
    public sealed class AddSonarrSeriesCmdlet : SonarrApiCmdletBase
    {
        readonly List<AddSeriesObject> _list;
        Range _range;

        MetadataTag Tag { get; }

        public AddSonarrSeriesCmdlet()
            : base()
        {
            _list = new(1);
            this.Tag = this.Services.GetRequiredService<MetadataResolver>()[Meta.SERIES];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public AddSeriesObject[] InputObject
        {
            get => Array.Empty<AddSeriesObject>();
            set
            {
                value ??= Array.Empty<AddSeriesObject>();
                int count = _list.Count;
                int howMany = value.Length;
                _range = new Range(count, howMany);

                _list.AddRange(value);
            }
        }

        [Parameter(Mandatory = false)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int ProfileId { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "AbsolutePath")]
        [ValidateNotNullOrEmpty]
        public string AbsolutePath { get; set; } = string.Empty;

        [Parameter(Mandatory = true, ParameterSetName = "RootFolderPath")]
        [ValidateNotNullOrEmpty]
        public string RootFolderPath { get; set; } = string.Empty;

        [Parameter(Mandatory = false)]
        public SwitchParameter IsMonitored { get; set; }

        [Parameter(Mandatory = false)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int QualityProfileId { get; set; }

        [Parameter(Mandatory = false)]
        [ValidateSet("Anime", "Standard", "Daily")]
        public string SeriesType { get; set; } = string.Empty;

        [Parameter(Mandatory = false)]
        public SwitchParameter UseSeasonFolders { get; set; }

        protected override ErrorRecord? Process()
        {
            var range = _list.GetRange(_range.Start.Value, _range.End.Value);

            foreach (var pso in range)
            {
                this.SetPath(pso);
                pso.AddOptions = new();

                if (this.HasParameter(x => x.UseSeasonFolders))
                {
                    pso.UseSeasonFolders = this.UseSeasonFolders.ToBool();
                }

                if (this.HasParameter(x => x.ProfileId))
                {
                    pso.ProfileId = this.ProfileId;
                }

                if (this.HasParameter(x => x.QualityProfileId))
                {
                    pso.QualityProfileId = this.QualityProfileId;
                }

                if (this.HasParameter(x => x.SeriesType))
                {
                    pso.SeriesType = this.SeriesType;
                }

                if (this.HasParameter(x => x.IsMonitored))
                {
                    pso.IsMonitored = this.IsMonitored.ToBool();
                }
            }

            return null;
        }
        protected override ErrorRecord? End()
        {
            string url = this.Tag.UrlBase;
            foreach (AddSeriesObject pso in _list)
            {
                this.SerializeIfDebug(pso, options: this.Services.GetService<SonarrJsonOptions>()?.GetForDebugging());

                if (this.ShouldProcess(pso.Title, "Adding Series"))
                {
                    var response = this.SendPostRequest<AddSeriesObject, SeriesObject>(url, pso);
                    var error = this.WriteSonarrResult(response);
                    if (error is not null)
                    {
                        this.WriteError(error);
                    }
                }
            }

            return null;
        }

        private void SetPath(AddSeriesObject pso)
        {
            if (this.HasParameter(x => x.RootFolderPath))
            {
                pso.Path = this.RootFolderPath;
                return;
            }

            pso.Path = this.AbsolutePath;
            pso.IsFullPath = true;
        }
    }
}
