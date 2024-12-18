using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Internal;
using MG.Sonarr.Next.Shell.Models.Series;
using MG.Sonarr.Next.Unions;
using System.Runtime.InteropServices;
using System.Security.Policy;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Add, "SonarrSeries", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
        DefaultParameterSetName = "RootFolderPath")]
    [MetadataCanPipe(Tag = Meta.SERIES_ADD)]
    public sealed class AddSonarrSeriesCmdlet : SonarrApiCmdletBase//, IDynamicParameters
    {
        List<AddSeriesObject> _list = null!;
        Range _range;
        private EditableSeriesAddOptions? _addOptions;
        private SeriesAddOptions? _usingOptions;
        internal SeriesAddOptions AddOptions => _usingOptions ??= _addOptions ?? SeriesAddOptions.Default;
        MetadataTag Tag { get; set; } = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNull]
        public AddSeriesObject[] InputObject
        {
            get => [];
            set
            {
                value ??= [];
                _list ??= new(value.Length);
                int count = _list.Count;
                int howMany = value.Length;
                _range = new Range(count, howMany);

                _list.AddRange(value);
            }
        }

        [Parameter(Mandatory = true, ParameterSetName = "AbsolutePath")]
        [ValidateNotNullOrWhiteSpace]
        public string AbsolutePath { get; set; } = string.Empty;

        [Parameter(Mandatory = false)]
        public SwitchParameter IsMonitored { get; set; }

        [Parameter(Mandatory = false)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int QualityProfileId { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "RootFolderPath")]
        [ValidateNotNullOrWhiteSpace]
        public string RootFolderPath { get; set; } = string.Empty;

        [Parameter]
        [System.Management.Automation.AllowNull]
        [AllowEmptyCollection]
        public SeriesAddIgnoreAction[] SearchForMissingEpisodes { get; set; } = [];

        [Parameter(Mandatory = false)]
        public string SeriesType { get; set; } = string.Empty;

        [Parameter(Mandatory = false)]
        public SwitchParameter UseSeasonFolders { get; set; }

        //const string WITH_FILES = "SearchEpisodesWithFiles";
        //const string WITHOUT_FILES = "SearchEpisodesWithoutFiles";
        //static readonly Lazy<RuntimeDefinedParameterDictionary> _runtimeDic = new(CreateRuntimeDictionary);
        //public object? GetDynamicParameters()
        //{
        //    RuntimeDefinedParameterDictionary? dict = null;
        //    if (this.SearchForMissingEpisodes)
        //    {
        //        dict = _runtimeDic.Value;
        //    }

        //    return dict;
        //}

        //private static RuntimeDefinedParameterDictionary CreateRuntimeDictionary()
        //{
        //    return new RuntimeDefinedParameterDictionary
        //        {
        //            {
        //                WITH_FILES,
        //                new RuntimeDefinedParameter()
        //                {
        //                    Attributes =
        //                    {
        //                        new ParameterAttribute() { Mandatory = false },
        //                    },
        //                    Name = WITH_FILES,
        //                    ParameterType = typeof(SwitchParameter),
        //                }
        //            },
        //            {
        //                WITHOUT_FILES,
        //                new RuntimeDefinedParameter()
        //                {
        //                    Attributes =
        //                    {
        //                        new ParameterAttribute() { Mandatory = false },
        //                    },
        //                    Name = WITHOUT_FILES,
        //                    ParameterType = typeof(SwitchParameter),
        //                }
        //            }
        //        };
        //}

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Tag = provider.GetRequiredService<IMetadataResolver>()[Meta.SERIES];
        }
        protected override void Begin(IServiceProvider provider)
        {
            if (this.HasParameter(this.SearchForMissingEpisodes) && this.SearchForMissingEpisodes.Length > 0)
            {
                int actions = this.SearchForMissingEpisodes.Distinct().Sum(x => (int)x);

                _addOptions = new EditableSeriesAddOptions
                {
                    IgnoreEpsWithFiles = actions > 0,
                    IgnoreEpsWithoutFiles = actions > 1,
                    SearchForMissingEps = true,
                };
            }

            _usingOptions = _addOptions;
        }
        protected override void Process(IServiceProvider provider)
        {
            foreach (AddSeriesObject pso in this.InputObject)
            {
                this.SetPath(pso);
                this.SetPropertiesFromParameters(pso, this.AddOptions);

                this.SerializeIfDebug(pso);

                if (this.ShouldProcess(pso.Title, "Adding Series"))
                {
                    Either<SeriesObject, SonarrErrorRecord> response = this.SendPostRequest<AddSeriesObject, SeriesObject>(this.Tag.UrlBase, pso);

                    this.WriteOutcome(pso, response);
                }
            }
        }

        private void SetPropertiesFromParameters(AddSeriesObject pso, SeriesAddOptions options)
        {
            pso.AddOptions = options;

            if (this.HasParameter(x => x.UseSeasonFolders, onlyIfPresent: true))
            {
                pso.UseSeasonFolders = this.UseSeasonFolders.ToBool();
            }

            if (this.HasParameter(this.ProfileId))
            {
                pso.ProfileId = this.ProfileId;
            }

            if (this.HasParameter(this.QualityProfileId))
            {
                pso.QualityProfileId = this.QualityProfileId;
            }

            if (this.HasParameter(this.SeriesType))
            {
                pso.SeriesType = this.SeriesType;
            }

            if (this.HasParameter(x => x.IsMonitored, onlyIfPresent: true))
            {
                pso.IsMonitored = this.IsMonitored.ToBool();
            }
        }
        private void SetPath(AddSeriesObject pso)
        {
            if (this.HasParameter(this.RootFolderPath))
            {
                pso.Path = this.RootFolderPath;
                return;
            }

            pso.Path = this.AbsolutePath;
            pso.IsFullPath = true;
        }
    }
}
