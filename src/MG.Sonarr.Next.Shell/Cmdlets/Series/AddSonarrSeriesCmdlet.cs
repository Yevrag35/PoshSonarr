using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Internal;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series;

/// <summary>
/// Represents the <c>Add-SonarrSeries</c> cmdlet, which adds series that a lookup found to Sonarr's library.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes the lookup results that <c>Search-SonarrSeries</c> writes, as <see cref="AddSeriesObject"/>
/// objects, from the pipeline or through <c>-InputObject</c>. For each one, it applies the path and the other
/// parameters that were given, posts the series to Sonarr, and writes the <see cref="SeriesObject"/> that Sonarr
/// returns for the new series to the pipeline. When a post fails, the cmdlet writes its error, unless the error is
/// ignorable, and goes on to the next series.
/// </para>
/// <para>
/// <c>-RootFolderPath</c>, the default parameter set, puts the series in a folder of its own under a root folder,
/// and <c>-AbsolutePath</c> gives the series' own folder instead. One of the two is required.
/// </para>
/// <para>
/// The cmdlet confirms each series before it posts it. Its confirm impact is low, so it prompts only when
/// <c>-Confirm</c> or <c>$ConfirmPreference</c> asks for it, and <c>-WhatIf</c> shows what it would add and adds
/// nothing.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Add, "SonarrSeries", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
	DefaultParameterSetName = "RootFolderPath")]
[MetadataCanPipe(Tag = Meta.SeriesAdd)]
public sealed class AddSonarrSeriesCmdlet : SonarrApiCmdletBase//, IDynamicParameters
{
	private EditableSeriesAddOptions? _addOptions;
	private SeriesAddOptions? _usingOptions;
	/// <summary>
	/// Gets the add options that the cmdlet gives each series that it posts.
	/// </summary>
	/// <remarks>
	/// The first read keeps the options that <see cref="Begin(IServiceProvider)"/> built from
	/// <c>-SearchForMissingEpisodes</c>, or <see cref="SeriesAddOptions.Default"/> when that parameter wasn't given
	/// or was empty.
	/// </remarks>
	/// <value>The options from <c>-SearchForMissingEpisodes</c>, or <see cref="SeriesAddOptions.Default"/>.</value>
	internal SeriesAddOptions AddOptions => _usingOptions ??= _addOptions ?? SeriesAddOptions.Default;
	/// <summary>
	/// Gets or sets the metadata tag of the series endpoint, which the cmdlet posts the series to.
	/// </summary>
	/// <value>The <c>#series</c> tag, set when the cmdlet's scope is created.</value>
	private MetadataTag Tag { get; set; } = null!;
	/// <summary>
	/// Gets a value that indicates whether the HTTP pipeline writes each request's JSON body to the debug stream before
	/// it sends the request.
	/// </summary>
	/// <remarks>
	/// The cmdlet writes each series' body to the debug stream itself, once the series is confirmed, so the HTTP
	/// pipeline doesn't write it again.
	/// </remarks>
	/// <value>Always <see langword="false"/>.</value>
	public override bool CanDebugSerializeBefore => false;

	/// <summary>
	/// Gets or sets the lookup results to add to the library.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The parameter takes pipeline input, in every parameter set. Each piped object arrives in an array of its own.
	/// </para>
	/// <para>
	/// The parameter's type is a model class with a public parameterless constructor, so PowerShell also builds a new
	/// <see cref="AddSeriesObject"/> from a hashtable or a custom object that it receives. Such an object holds only the
	/// keys or properties that it was given, so pass the objects that <c>Search-SonarrSeries</c> writes.
	/// </para>
	/// </remarks>
	/// <value>The lookup results. The default is an empty array.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ValueFromPipeline = true)]
	[ValidateNotNull]
	public AddSeriesObject[] InputObject { get; set; } = [];

	/// <summary>
	/// Gets or sets the path of the folder that holds the series' files.
	/// </summary>
	/// <remarks>
	/// The path is the series' own folder, as Sonarr's server sees it, and every series that the cmdlet adds gets it.
	/// The parameter can't be combined with <c>-RootFolderPath</c>.
	/// </remarks>
	/// <value>The full path of the series' folder. The default is an empty string.</value>
	[Parameter(Mandatory = true, ParameterSetName = "AbsolutePath")]
	[ValidateNotNullOrWhiteSpace]
	public string AbsolutePath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets a value that indicates whether Sonarr monitors the series that it adds.
	/// </summary>
	/// <remarks>
	/// When the switch is left out, each series keeps the value that its lookup result holds, and
	/// <c>-IsMonitored:$false</c> adds the series unmonitored.
	/// </remarks>
	/// <value>The switch, which is present to monitor the series.</value>
	[Parameter(Mandatory = false)]
	public SwitchParameter IsMonitored { get; set; }

	/// <summary>
	/// Gets or sets the ID of the quality profile that the series use.
	/// </summary>
	/// <remarks>
	/// When the parameter is left out, each series keeps the profile ID that its lookup result holds.
	/// </remarks>
	/// <value>The quality profile's ID, which has to be greater than zero. The default is 0, for none given.</value>
	[Parameter(Mandatory = false)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int QualityProfileId { get; set; }

	/// <summary>
	/// Gets or sets the path of the root folder that the series' folders go under.
	/// </summary>
	/// <remarks>
	/// Sonarr creates a folder for each series under the root folder. The parameter can't be combined with
	/// <c>-AbsolutePath</c>.
	/// </remarks>
	/// <value>The root folder's path, as Sonarr's server sees it. The default is an empty string.</value>
	[Parameter(Mandatory = true, ParameterSetName = "RootFolderPath")]
	[ValidateNotNullOrWhiteSpace]
	public string RootFolderPath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the actions that set whether Sonarr searches for the series' missing episodes after it adds the
	/// series, and which episodes it leaves unmonitored.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Any value makes Sonarr search for missing episodes once it adds the series. <c>Default</c> leaves every episode
	/// monitored, <c>IgnoreWithEpisodes</c> leaves unmonitored the episodes that already have files, and
	/// <c>IgnoreWithoutEpisodes</c> the episodes that don't have files yet. Each <c>Ignore</c> value sets only its own
	/// half, so the two together leave both unmonitored, and <c>Default</c> given with either changes nothing. A value
	/// given more than once binds once.
	/// </para>
	/// <para>
	/// When the parameter is left out, or is <see langword="null"/> or empty, the series get the default add options,
	/// which don't search for missing episodes.
	/// </para>
	/// </remarks>
	/// <value>The actions, or <see langword="null"/> or empty for the default add options.</value>
	[Parameter]
	[System.Management.Automation.AllowNull]
	[AllowEmptyCollection]
	[DistinctValues(typeof(SeriesAddIgnoreAction))]
	public SeriesAddIgnoreAction[]? SearchForMissingEpisodes { get; set; }

	/// <summary>
	/// Gets or sets the type of the series, such as <c>Standard</c>, <c>Daily</c>, or <c>Anime</c>.
	/// </summary>
	/// <remarks>
	/// When the parameter is left out, each series keeps the type that its lookup result holds.
	/// </remarks>
	/// <value>The series type's name. The default is an empty string, for none given.</value>
	[Parameter(Mandatory = false)]
	public string SeriesType { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets a value that indicates whether Sonarr keeps each season's files in a folder of its own.
	/// </summary>
	/// <remarks>
	/// When the switch is left out, each series keeps the value that its lookup result holds, and
	/// <c>-UseSeasonFolders:$false</c> turns season folders off.
	/// </remarks>
	/// <value>The switch, which is present to use season folders.</value>
	[Parameter(Mandatory = false)]
	public SwitchParameter UseSeasonFolders { get; set; }

	/// <summary>
	/// Runs the base class's setup for the scope, then gets the metadata tag of the series endpoint.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		this.Tag = provider.GetRequiredService<IMetadataResolver>()[Meta.Series];
	}
	/// <summary>
	/// Builds the add options from <c>-SearchForMissingEpisodes</c>, when it holds a value, for every series that the
	/// cmdlet posts.
	/// </summary>
	/// <remarks>
	/// Each of the two ignore options is set when its own value is in the parameter, whatever the other values are.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Begin(IServiceProvider provider)
	{
		if (this.HasNotNullParameter(SearchForMissingEpisodes) && this.SearchForMissingEpisodes.Length > 0)
		{
			SeriesAddIgnoreAction[] actions = this.SearchForMissingEpisodes;

			_addOptions = new EditableSeriesAddOptions
			{
				IgnoreEpsWithFiles = actions.Contains(SeriesAddIgnoreAction.IgnoreWithEpisodes),
				IgnoreEpsWithoutFiles = actions.Contains(SeriesAddIgnoreAction.IgnoreWithoutEpisodes),
				SearchForMissingEps = true,
			};
		}

		_usingOptions = _addOptions;
	}
	/// <summary>
	/// Applies the path and the other parameters to each lookup result, posts the confirmed ones to Sonarr, and writes
	/// the series that Sonarr adds to the pipeline.
	/// </summary>
	/// <remarks>
	/// Each series' JSON body goes to the debug stream once the series is confirmed. A lookup result is committed after
	/// a post that succeeds and reset after one that fails, so it looks the same afterward as before, and a failed
	/// post's error is written unless it's ignorable.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		foreach (AddSeriesObject pso in this.InputObject)
		{
			this.SetPath(pso);
			this.SetPropertiesFromParameters(pso, this.AddOptions);

			if (this.ShouldProcess(pso.Title, "Adding Series"))
			{
				this.SerializeIfDebug(pso);

				var response = this.SendPostRequest<SeriesObject>(this.Tag.UrlBase, pso);

				// WriteOutcome resets the lookup result when the add fails, so commit it when the add succeeds, which
				// takes out what OnSerializing wrote either way.
				if (response.IsT1)
				{
					pso.Commit();
				}

				this.WriteOutcome(pso, response);
			}
		}
	}

	/// <summary>
	/// Sets the specified add options on the specified lookup result, along with the values of the parameters that
	/// were given: <c>-UseSeasonFolders</c>, <c>-QualityProfileId</c>, <c>-SeriesType</c>, and <c>-IsMonitored</c>.
	/// </summary>
	/// <remarks>
	/// A parameter that wasn't given leaves the lookup result's own value.
	/// </remarks>
	/// <param name="pso">The lookup result to post.</param>
	/// <param name="options">The add options that the series is posted with.</param>
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
	/// <summary>
	/// Sets the specified lookup result's path from <c>-RootFolderPath</c>, as a root folder, or else from
	/// <c>-AbsolutePath</c>, as the series' own folder.
	/// </summary>
	/// <param name="pso">The lookup result to post.</param>
	private void SetPath(AddSeriesObject pso)
	{
		if (this.HasParameter(this.RootFolderPath))
		{
			pso.Path = this.RootFolderPath;
			pso.IsFullPath = false;
			return;
		}

		pso.Path = this.AbsolutePath;
		pso.IsFullPath = true;
	}
}
