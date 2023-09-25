using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Series;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Models.Series;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Add, "SonarrSeries", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
        DefaultParameterSetName = "RootFolderPath")]
    [CmdletBinding(DefaultParameterSetName = "RootFolderPath")]
    public sealed class AddSonarrSeriesCmdlet : SonarrApiCmdletBase, IDynamicParameters
    {
        readonly List<AddSeriesObject> _list;
        Range _range;
        SeriesAddOptions AddOptions { get; }

        MetadataTag Tag { get; }

        public AddSonarrSeriesCmdlet()
            : base()
        {
            _list = new(1);
            this.AddOptions = new();
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

        [Parameter(Mandatory = true, ParameterSetName = "AbsolutePath")]
        [Parameter(Mandatory = true, ParameterSetName = "AbsolutePathAndSearch")]
        [ValidateNotNullOrEmpty]
        public string AbsolutePath { get; set; } = string.Empty;

        [Parameter(Mandatory = false)]
        public SwitchParameter IsMonitored { get; set; }

        [Parameter(Mandatory = false)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int ProfileId { get; set; }

        [Parameter(Mandatory = false)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int QualityProfileId { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "RootFolderPath")]
        [Parameter(Mandatory = true, ParameterSetName = "RootFolderPathAndSearch")]
        [ValidateNotNullOrEmpty]
        public string RootFolderPath { get; set; } = string.Empty;

        [Parameter(Mandatory = true, ParameterSetName = "AbsolutePathAndSearch")]
        [Parameter(Mandatory = true, ParameterSetName = "RootFolderPathAndSearch")]
        public SwitchParameter SearchForMissingEpisodes
        {
            get => this.AddOptions.SearchForMissingEpisodes;
            set => this.AddOptions.SearchForMissingEpisodes = value.ToBool();
        }

        [Parameter(Mandatory = false)]
        public string SeriesType { get; set; } = string.Empty;

        [Parameter(Mandatory = false)]
        public SwitchParameter UseSeasonFolders { get; set; }

        const string WITH_FILES = "SearchEpisodesWithFiles";
        const string WITHOUT_FILES = "SearchEpisodesWithoutFiles";
        public object? GetDynamicParameters()
        {
            RuntimeDefinedParameterDictionary? dict = null;
            if (this.SearchForMissingEpisodes)
            {
                dict = new RuntimeDefinedParameterDictionary
                {
                    {
                        WITH_FILES,
                        new RuntimeDefinedParameter()
                        {
                            Attributes =
                            {
                                new ParameterAttribute() { Mandatory = false },
                            },
                            Name = WITH_FILES,
                            ParameterType = typeof(SwitchParameter),
                        }
                    },
                    {
                        WITHOUT_FILES,
                        new RuntimeDefinedParameter()
                        {
                            Attributes =
                            {
                                new ParameterAttribute() { Mandatory = false },
                            },
                            Name = WITHOUT_FILES,
                            ParameterType = typeof(SwitchParameter),
                        }
                    }
                };
            }

            return dict;
        }

        protected override void Process()
        {
            var range = _list.GetRange(_range.Start.Value, _range.End.Value);

            foreach (var pso in range)
            {
                this.SetPath(pso);
                pso.AddOptions = this.AddOptions;

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
        }
        protected override void End()
        {
            this.SetAddOptions(this.AddOptions);
            string url = this.Tag.UrlBase;

            foreach (AddSeriesObject pso in _list)
            {
                this.SerializeIfDebug(pso, options: this.Services.GetService<SonarrJsonOptions>()?.GetForDebugging());

                if (this.ShouldProcess(pso.Title, "Adding Series"))
                {
                    var response = this.SendPostRequest<AddSeriesObject, SeriesObject>(url, pso);
                    if (response.TryPickT0(out SeriesObject? so, out var error))
                    {
                        this.WriteObject(so);
                    }
                    else
                    {
                        this.WriteConditionalError(error);
                    }
                }
            }
        }

        private void SetAddOptions(SeriesAddOptions options)
        {
            if (this.SearchForMissingEpisodes)
            {
                if (this.MyInvocation.BoundParameters.TryGetValue(WITH_FILES, out object? wf) 
                    &&
                    wf is SwitchParameter wfSwitch)
                {
                    options.IgnoreEpisodesWithFiles = !wfSwitch.ToBool();
                }

                if (this.MyInvocation.BoundParameters.TryGetValue(WITHOUT_FILES, out object? wof)
                    &&
                    wof is SwitchParameter wofSwitch)
                {
                    options.IgnoreEpisodesWithoutFiles = !wofSwitch.ToBool();
                }
            }
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
