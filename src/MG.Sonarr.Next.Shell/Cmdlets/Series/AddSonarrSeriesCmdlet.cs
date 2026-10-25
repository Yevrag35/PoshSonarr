using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Internal;
using MG.Sonarr.Next.Shell.Models.Series;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series;

[Cmdlet(VerbsCommon.Add, "SonarrSeries", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
	DefaultParameterSetName = "RootFolderPath")]
[MetadataCanPipe(Tag = Meta.SERIES_ADD)]
public sealed class AddSonarrSeriesCmdlet : SonarrApiCmdletBase//, IDynamicParameters
{
	private EditableSeriesAddOptions? _addOptions;
	private SeriesAddOptions? _usingOptions;
	internal SeriesAddOptions AddOptions => _usingOptions ??= _addOptions ?? SeriesAddOptions.Default;
	private MetadataTag Tag { get; set; } = null!;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ValueFromPipeline = true)]
	[ValidateNotNull]
	public AddSeriesObject[] InputObject { get; set; } = [];

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
	[DistinctValues(typeof(SeriesAddIgnoreAction))]
	public SeriesAddIgnoreAction[]? SearchForMissingEpisodes { get; set; }

	[Parameter(Mandatory = false)]
	public string SeriesType { get; set; } = string.Empty;

	[Parameter(Mandatory = false)]
	public SwitchParameter UseSeasonFolders { get; set; }

	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		this.Tag = provider.GetRequiredService<IMetadataResolver>()[Meta.SERIES];
	}
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Begin(IServiceProvider provider)
	{
		if (this.HasNotNullParameter(SearchForMissingEpisodes) && this.SearchForMissingEpisodes.Length > 0)
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

	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	private void SetPropertiesFromParameters(AddSeriesObject pso, SeriesAddOptions options)
	{
		pso.AddOptions = options;

		if (this.UseSeasonFolders.IsPresent)
		{
			pso.UseSeasonFolders = this.UseSeasonFolders.ToBool();
		}

		if (this.HasParameter(QualityProfileId))
		{
			pso.QualityProfileId = this.QualityProfileId;
		}

		if (this.HasParameter(SeriesType))
		{
			pso.SeriesType = this.SeriesType;
		}

		if (this.IsMonitored.IsPresent)
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
