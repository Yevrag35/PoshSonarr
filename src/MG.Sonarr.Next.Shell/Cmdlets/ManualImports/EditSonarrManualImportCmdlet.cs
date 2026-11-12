using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.Services;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.ManualImports;
using MG.Sonarr.Next.Models.Qualities;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets.ManualImports;

/// <summary>
/// Represents the <c>Edit-SonarrManualImport</c> cmdlet, which sets the series, episode, or quality of the files that
/// <c>Get-SonarrManualImport</c> found, before <c>Invoke-SonarrManualImport</c> imports them.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet changes each <see cref="ManualImportObject"/> piped to <c>-InputObject</c> in place, and sends Sonarr
/// nothing but the requests that look up what an ID stands for. <c>-Series</c> sets the file's series, <c>-Episode</c>
/// adds an episode to its episodes, and <c>-Quality</c> sets its quality. Each takes an ID or the object itself, and
/// the cmdlet looks up each ID once, when it begins, so every file gets the same values.
/// </para>
/// <para>
/// The cmdlet writes each file to the pipeline only with <c>-PassThru</c>. It confirms nothing, since it changes
/// nothing in Sonarr.
/// </para>
/// </remarks>
[Cmdlet(VerbsData.Edit, "SonarrManualImport")]
[MetadataCanPipe(Tag = Meta.ManualImport)]
public sealed class EditSonarrManualImportCmdlet : SonarrApiCmdletBase
{
	private Either<int, SeriesObject> _series;
	private Either<int, EpisodeObject> _episode;
	private Either<int, QualityRevisionObject> _quality;

	/// <summary>
	/// Gets or sets the file to edit.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, and the file's ID has to be greater than zero. Its alias is <c>-Record</c>.
	/// </remarks>
	/// <value>The file that Sonarr found for a manual import.</value>
	[Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
	[Alias("Record")]
	[ValidateNotNull]
	[ValidateId(ValidateRangeKind.Positive)]
	public ManualImportObject InputObject { get; set; } = null!;

	/// <summary>
	/// Gets or sets the series that the files are imported to.
	/// </summary>
	/// <remarks>
	/// The argument is a series' ID, which has to be greater than zero and is looked up when the cmdlet begins, or a
	/// <see cref="SeriesObject"/> itself.
	/// </remarks>
	/// <value>The series' ID or the series.</value>
	[Parameter]
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[IdOrObjectTransform<SeriesObject>]
	[ValidateId(ValidateRangeKind.Positive)]
	public Either<int, SeriesObject> Series
	{
		get => _series;
		set => _series = value;
	}

	/// <summary>
	/// Gets or sets the episode that is added to the files' episodes.
	/// </summary>
	/// <remarks>
	/// The argument is an episode's ID, which has to be greater than zero and is looked up when the cmdlet begins, or
	/// an <see cref="EpisodeObject"/> itself.
	/// </remarks>
	/// <value>The episode's ID or the episode.</value>
	[Parameter]
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[IdOrObjectTransform<EpisodeObject>]
	[ValidateId(ValidateRangeKind.Positive)]
	public Either<int, EpisodeObject> Episode
	{
		get => _episode;
		set => _episode = value;
	}

	// A quality revision has no ID of its own, so only an ID is range-checked.
	/// <summary>
	/// Gets or sets the quality that the files are imported as.
	/// </summary>
	/// <remarks>
	/// The argument is the ID of a quality, which has to be greater than zero, or a <see cref="QualityRevisionObject"/>
	/// itself, which has no ID to check. An ID is looked up among Sonarr's quality definitions when the cmdlet begins,
	/// and becomes a revision of that quality.
	/// </remarks>
	/// <value>The quality's ID or the quality revision.</value>
	[Parameter]
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[IdOrObjectTransform<QualityRevisionObject>]
	[ValidateId(ValidateRangeKind.Positive, NullBehavior = InputNullBehavior.Ignore)]
	public Either<int, QualityRevisionObject> Quality
	{
		get => _quality;
		set => _quality = value;
	}

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet writes each edited file to the pipeline.
	/// </summary>
	/// <value>The switch, which is present to write the edited files.</value>
	[Parameter]
	public SwitchParameter PassThru { get; set; }

	/// <summary>
	/// Resolves the episode, quality, and series that the parameters give, looking up any that are given by ID, and
	/// stores them in the scope's edit, which applies them to each file.
	/// </summary>
	/// <remarks>
	/// When a lookup fails, the cmdlet writes its error, unless it's an ignorable error for a series or an episode, and
	/// the files don't get that value.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Begin(IServiceProvider provider)
	{
		var edit = provider.GetRequiredService<ManualImportEdit>();
		if (this.HasParameter(Episode))
		{
			edit.Episode = this.GetObject(_episode, provider.GetMetadataTag(Meta.Episode));
		}

		if (this.HasParameter(Quality))
		{
			edit.Quality = this.GetQualityRevision(_quality, provider.GetMetadataTag(Meta.Quality));
		}

		if (this.HasParameter(Series))
		{
			edit.Series = this.GetObject(_series, provider.GetMetadataTag(Meta.Series));
		}
	}

	/// <summary>
	/// Applies the resolved series, episode, and quality to the piped file, and writes the file to the pipeline with
	/// <c>-PassThru</c>.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		var edit = provider.GetRequiredService<ManualImportEdit>();
		edit.EditImport(this.InputObject);

		if (this.PassThru)
		{
			this.WriteObject(this.InputObject);
		}
	}

	/// <summary>
	/// Returns the object that the specified union holds, or gets the object for the ID that it holds from the
	/// endpoint of the specified tag.
	/// </summary>
	/// <remarks>
	/// When the request fails, the method writes its error, unless it's ignorable, as the one for an ID that doesn't
	/// exist is.
	/// </remarks>
	/// <typeparam name="T">The type of the object, such as <see cref="SeriesObject"/> or
	/// <see cref="EpisodeObject"/>.</typeparam>
	/// <param name="oneOf">The ID or the object, as the parameter bound it.</param>
	/// <param name="tag">The metadata tag of the endpoint that the ID is looked up at.</param>
	/// <returns>The object, or <see langword="null"/> when the request failed.</returns>
	private T? GetObject<T>(Either<int, T> oneOf, MetadataTag tag)
		where T : class
	{
		if (oneOf.TryGetT2(out T? value, out int id))
		{
			return value;
		}

		string url = tag.GetUrlForId(id);
		var response = this.SendGetRequest<T>(url);
		if (response.IsError)
		{
			this.WriteConditionalError(response.Error);
			return default;
		}
		else
		{
			return response.Value;
		}
	}

	/// <summary>
	/// Returns the quality revision that the specified union holds, or builds one from the quality whose ID the union
	/// holds, found among Sonarr's quality definitions.
	/// </summary>
	/// <remarks>
	/// The method gets every quality definition with one request, and matches the ID against the <c>Quality</c>
	/// property of each. When the request fails, it writes the error, whether or not it's ignorable.
	/// </remarks>
	/// <param name="oneOf">The quality's ID or the quality revision, as the parameter bound it.</param>
	/// <param name="tag">The metadata tag whose endpoint lists the quality definitions.</param>
	/// <returns>
	/// The quality revision, or <see langword="null"/> when the request failed or no definition has a quality with
	/// the ID.
	/// </returns>
	private QualityRevisionObject? GetQualityRevision(Either<int, QualityRevisionObject> oneOf, MetadataTag tag)
	{
		if (oneOf.TryGetT2(out QualityRevisionObject? qualityObj, out int qualityId))
		{
			return qualityObj;
		}

		string url = tag.UrlBase;
		var definitions = this.SendGetRequest<MetadataList<QualityDefinitionObject>>(url);
		if (definitions.IsError)
		{
			this.WriteError(definitions.Error);
			return null;
		}

		foreach (QualityDefinitionObject definition in definitions.Value)
		{
			PSPropertyInfo? info = definition.Properties[nameof(this.Quality)];
			if (info?.Value is QualityObject quality && quality.Id == qualityId)
			{
				return new QualityRevisionObject(quality);
			}
		}

		return null;
	}
}