using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Renames;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Commands;

/// <summary>
/// Represents the <c>Test-SonarrRename</c> cmdlet, which gets the episode files of a series that Sonarr would rename
/// to match its naming format.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes the series by <c>-SeriesId</c>, or from an object piped to <c>-InputObject</c> as an
/// <see cref="ISeriesPipeable"/> value, such as a series, an episode, an episode file, or a calendar entry, and
/// <c>-SeasonNumber</c> limits the files to one season. For each series, it sends one request and writes each file
/// that would be renamed to the pipeline as a <see cref="RenameObject"/>, which <c>Invoke-SonarrRename</c> takes from
/// the pipeline. When a request fails, it writes the error, unless it's ignorable.
/// </para>
/// <para>
/// The cmdlet changes nothing in Sonarr.
/// </para>
/// </remarks>
[Cmdlet(VerbsDiagnostic.Test, "SonarrRename")]
[MetadataCanPipe(Tag = Meta.Calendar)]
[MetadataCanPipe(Tag = Meta.Episode)]
[MetadataCanPipe(Tag = Meta.EpisodeFile)]
[MetadataCanPipe(Tag = Meta.Series)]
public sealed class TestSonarrRenameCmdlet : SonarrApiCmdletBase
{
	QueryCol _params = null!;

	/// <summary>
	/// Gets or sets the object whose series' episode files are tested.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, and the object's series ID has to be greater than zero. Setting it sets
	/// <see cref="SeriesId"/> to the object's series ID, and the getter always returns <see langword="null"/>.
	/// </remarks>
	/// <value>Always <see langword="null"/> when read.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "BySeriesInput")]
	[ValidateId<ISeriesPipeable>(ValidateRangeKind.Positive)]
	public ISeriesPipeable InputObject
	{
		get => null!;
		set => this.SeriesId = value.SeriesId;
	}

	/// <summary>
	/// Gets or sets the ID of the series whose episode files are tested.
	/// </summary>
	/// <remarks>
	/// The ID has to be greater than zero. An object piped to <c>-InputObject</c> sets it to the object's series ID.
	/// </remarks>
	/// <value>The series' ID.</value>
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByExplicitSeriesId")]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int SeriesId { get; set; }

	/// <summary>
	/// Gets or sets the number of the season whose episode files are tested.
	/// </summary>
	/// <remarks>
	/// The number can't be negative, and season 0 holds a series' specials. The parameter's alias is <c>-Season</c>.
	/// </remarks>
	/// <value>The season number. When the parameter isn't given, every season's files are tested.</value>
	[Parameter(Position = 0, ParameterSetName = "BySeriesInput")]
	[Parameter(Position = 1, ParameterSetName = "ByExplicitSeriesId")]
	[Alias("Season")]
	[ValidateRange(ValidateRangeKind.NonNegative)]
	public int SeasonNumber { get; set; }

	/// <summary>
	/// Creates the query that the cmdlet's requests send.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		_params = new(10);
	}

	/// <summary>
	/// Adds the season number to the query when <c>-SeasonNumber</c> is given.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		if (this.HasParameter(this.SeasonNumber))
		{
			_params.Add("seasonNumber", this.SeasonNumber);
		}
	}

	/// <summary>
	/// Gets the files of the current series that Sonarr would rename and writes them to the pipeline.
	/// </summary>
	/// <remarks>
	/// The series ID is added to the query for this request only and removed afterward, so the next piped object
	/// sends its own. When the request fails, the cmdlet writes its error, unless it's ignorable.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		int index = _params.Count;
		_params.Add("seriesId", this.SeriesId);

		string url = GetUrl(_params);
		var response = this.SendGetRequest<MetadataList<RenameObject>>(url);
		_ = this.TryWriteObject(response);
		_params.RemoveAt(index);
	}

	/// <summary>
	/// Builds the path of the rename endpoint with the specified query.
	/// </summary>
	/// <remarks>
	/// The text is built in a stack buffer of up to 256 characters, or in a rented one when it's longer.
	/// </remarks>
	/// <param name="parameters">The query parameters to append.</param>
	/// <returns>The endpoint's relative path followed by the query.</returns>
	private static string GetUrl(QueryCol parameters)
	{
		const int max_stackalloc = 256;

		int length = Constants.Rename.Length + parameters.MaxLength + 1;
		using var builder = SpanStringBuilder.Create(
			length <= max_stackalloc
				? stackalloc char[length]
				: length);

		builder.Append(Constants.Rename);
		builder.Append(parameters, parameters.MaxLength);

		return builder.ToString();
	}
}
