using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Models.Series;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series
{
    [Cmdlet(VerbsCommon.Add, "SonarrSeries", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
        DefaultParameterSetName = "RootFolderPath")]
    [CmdletBinding(DefaultParameterSetName = "RootFolderPath")]
    public sealed class AddSonarrSeriesCmdlet : SonarrApiCmdletBase, IDynamicParameters
    {
        List<AddSeriesObject> _list = null!;
        Range _range;
        SeriesAddOptions AddOptions { get; set; } = null!;
        MetadataTag Tag { get; set; } = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public AddSeriesObject[] InputObject
        {
            get => Array.Empty<AddSeriesObject>();
            set
            {
                value ??= Array.Empty<AddSeriesObject>();
                _list ??= new(value.Length);
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

        [Parameter(Mandatory = true)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int LanguageProfileId { get; set; }

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
            get => this.AddOptions?.SearchForMissingEpisodes ?? default;
            set => this.SetAddOptionsValue(in value);
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

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Tag = provider.GetRequiredService<MetadataResolver>()[Meta.SERIES];
        }
        protected override void Process(IServiceProvider provider)
        {
            ReadOnlySpan<AddSeriesObject> span = CollectionsMarshal.AsSpan(_list).Slice(_range.Start.Value, _range.End.Value);

            foreach (AddSeriesObject pso in span)
            {
                this.SetPath(pso);
                this.SetPropertiesFromParameters(pso);
            }
        }

        private void SetPropertiesFromParameters(AddSeriesObject pso)
        {
            pso.AddOptions = this.AddOptions;
            pso.LanguageProfileId = this.LanguageProfileId;

            if (this.HasParameter(x => x.UseSeasonFolders, onlyIfPresent: true))
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

            if (this.HasParameter(x => x.IsMonitored, onlyIfPresent: true))
            {
                pso.IsMonitored = this.IsMonitored.ToBool();
            }
        }

        protected override void End(IServiceProvider provider)
        {
            this.SetAddOptions(this.AddOptions);
            string url = this.Tag.UrlBase;

            foreach (AddSeriesObject pso in _list)
            {
                this.SerializeIfDebug(pso, options: provider.GetService<SonarrJsonOptions>()?.GetForDebugging());

                if (this.ShouldProcess(pso.Title, "Adding Series"))
                {
                    var response = this.SendPostRequest<AddSeriesObject, SeriesObject>(url, pso);
                    if (response.TryPickT0(out SeriesObject? so, out var error))
                    {
                        this.WriteObject(so);
                        pso.Commit();
                    }
                    else
                    {
                        this.WriteConditionalError(error);
                        pso.Reset();
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

                if (this.MyInvocation.BoundParameters.TryGetValueAs(WITHOUT_FILES, out SwitchParameter wofSwitch))
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
        private void SetAddOptionsValue(in SwitchParameter swParam)
        {
            this.SetValue(
                value: swParam.ToBool(),
                getSetting: x => x.AddOptions,
                setValue: (x, options) => options.SearchForMissingEpisodes = x);
        }
    }
}
