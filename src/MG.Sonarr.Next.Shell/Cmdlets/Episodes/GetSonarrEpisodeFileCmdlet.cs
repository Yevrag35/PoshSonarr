using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Episodes;

/// <summary>
/// Represents the <c>Get-SonarrEpisodeFile</c> cmdlet, which gets the files of episodes by their IDs or by the
/// series that they belong to.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet collects its IDs until its input ends, and then writes each file to the pipeline as an
/// <see cref="EpisodeFileObject"/>. It gets a file by its ID with a request of its own, and a series' files with one
/// request for each series.
/// </para>
/// <para>
/// <c>-Id</c> takes episode file IDs, and an episode, an episode file, or a calendar entry piped to the cmdlet gets
/// the file that the object names. An episode or a calendar entry without a file gets a warning instead.
/// <c>-SeriesId</c> takes series IDs, and a series piped to the cmdlet gets every file of the series.
/// </para>
/// <para>
/// When a request fails, the cmdlet writes its error, unless the error is ignorable, as it is for an ID that Sonarr
/// doesn't have, and goes on to the next ID.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrEpisodeFile")]
[MetadataCanPipe(Tag = Meta.Calendar)]
[MetadataCanPipe(Tag = Meta.Episode)]
public sealed class GetSonarrEpisodeFileCmdlet : SonarrMetadataCmdlet
{
	SortedSet<int> _ids = null!;
	SortedSet<int> _seriesIds = null!;
	QueryCol _params = null!;

	/// <summary>
	/// Gets or sets the objects whose episode files the cmdlet gets.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, and each piped object arrives in an array of its own. It takes any object
	/// that implements <see cref="IEpisodeFilePipeable"/>, such as an episode, an episode file, or a calendar entry.
	/// An object's file ID can be 0, for an episode without a file, which the cmdlet skips with a warning, but not
	/// negative.
	/// </remarks>
	/// <value>The piped objects. The default is an empty array.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByEpisodeFileInput")]
	[ValidateIds<IEpisodeFilePipeable>(ValidateRangeKind.NonNegative)]
	public IEpisodeFilePipeable[] InputObject { get; set; } = Array.Empty<IEpisodeFilePipeable>();

	/// <summary>
	/// Gets or sets the series whose episode files the cmdlet gets.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, and each piped series arrives in an array of its own. It takes any object
	/// that implements <see cref="IEpisodeFileBySeriesPipeable"/>, such as a series, whose ID has to be greater than
	/// zero.
	/// </remarks>
	/// <value>The piped series. The default is an empty array.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "BySeriesInput")]
	[ValidateIds<IEpisodeFileBySeriesPipeable>(ValidateRangeKind.Positive)]
	public IEpisodeFileBySeriesPipeable[] SeriesInput { get; set; } = Array.Empty<IEpisodeFileBySeriesPipeable>();

	/// <summary>
	/// Gets or sets the IDs of the episode files to get.
	/// </summary>
	/// <value>The episode files' IDs, each greater than zero. The default is an empty array.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByEpisodeFileId")]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs of the series whose episode files the cmdlet gets.
	/// </summary>
	/// <value>The series' IDs, each greater than zero. The default is an empty array.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ParameterSetName = "BySeriesId")]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] SeriesId { get; set; } = Array.Empty<int>();

	/// <summary>
	/// Gets the metadata tag of the episode file endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The episode file's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.EpisodeFile];
	}
	/// <summary>
	/// Runs the base class's setup for the scope, then creates the sets of file and series IDs and the query that the
	/// cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
		_seriesIds = [];
		_params = new(10);
	}

	/// <summary>
	/// Determines whether the cmdlet collected neither a file ID nor a series ID.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if both sets of IDs are empty; otherwise, <see langword="false"/>.
	/// </returns>
	private bool HasNoParameters()
	{
		return _ids.Count <= 0
			   &&
			   _seriesIds.Count <= 0;
	}

	/// <summary>
	/// Adds the IDs from <c>-Id</c> and <c>-SeriesId</c> to the IDs that the cmdlet gets files for.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
		_seriesIds.UnionWith(this.SeriesId);
	}
	/// <summary>
	/// Adds the file IDs or the series IDs of the piped objects to those that the cmdlet gets files for, and writes a
	/// warning for each piped object without a file.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		if (this.HasParameter(this.InputObject))
		{
			EpisodeFileInput.AddFileIds(this, this.InputObject, _ids);
		}
		else if (this.HasParameter(this.SeriesInput))
		{
			_seriesIds.UnionWith(
				this.SeriesInput
					.Where(x => x.SeriesId > 0)
						.Select(x => x.SeriesId));
		}
	}
	/// <summary>
	/// Gets the files for the collected series IDs, in the series parameter sets, or for the collected file IDs
	/// otherwise, and writes them to the pipeline.
	/// </summary>
	/// <remarks>
	/// When no ID was collected, the cmdlet sends no request.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		if (this.HasNoParameters())
		{
			return;
		}

		List<EpisodeFileObject> files = ParameterNameStartsWithSeries(this.ParameterSetName)
			? this.GetEpFilesBySeriesId(_seriesIds)
			: this.GetEpFilesById(_ids);

		this.WriteCollection(files);
	}

	/// <summary>
	/// Gets the episode files with the specified IDs, with a request for each one.
	/// </summary>
	/// <remarks>
	/// When a request fails, the method writes its error, unless the error is ignorable, and goes on to the next ID.
	/// </remarks>
	/// <param name="fileIds">The IDs of the files to get, or <see langword="null"/> for none.</param>
	/// <returns>The files that were found, in the order of their IDs.</returns>
	private List<EpisodeFileObject> GetEpFilesById(IReadOnlySet<int>? fileIds)
	{
		List<EpisodeFileObject> list = [];
		if (fileIds is null)
		{
			return list;
		}

		foreach (int id in fileIds)
		{
			string url = this.Tag.GetUrlForId(id);
			var response = this.SendGetRequest<EpisodeFileObject>(url);
			if (response.IsError)
			{
				this.WriteConditionalError(response.Error);
				continue;
			}

			list.Add(response.Value);
		}

		return list;
	}
	/// <summary>
	/// Gets the episode files of each of the specified series, with a request for each series.
	/// </summary>
	/// <remarks>
	/// Each request carries the series' ID as its <c>seriesId</c> query parameter, and the query is cleared for the
	/// next series. When a request fails, the method writes its error, unless the error is ignorable, and goes on to
	/// the next series.
	/// </remarks>
	/// <param name="seriesIds">The IDs of the series, or <see langword="null"/> for none.</param>
	/// <returns>The files of every series, in the order of the series' IDs.</returns>
	private List<EpisodeFileObject> GetEpFilesBySeriesId(IReadOnlySet<int>? seriesIds)
	{
		List<EpisodeFileObject> list = [];
		if (seriesIds is null)
		{
			return list;
		}

		foreach (int id in seriesIds)
		{
			_params.Add(Constants.SeriesIdLowercase, id);
			string url = this.Tag.GetUrl(_params);
			var response = this.SendGetRequest<MetadataList<EpisodeFileObject>>(url);
			if (response.IsError)
			{
				this.WriteConditionalError(response.Error);
				continue;
			}

			list.AddRange(response.Value);

			_params.Clear();
		}

		return list;
	}
	/// <summary>
	/// Determines whether the specified parameter set's name starts with <c>BySeries</c>, ignoring case.
	/// </summary>
	/// <param name="setName">The name of the parameter set that PowerShell resolved.</param>
	/// <returns>
	/// <see langword="true"/> if the set takes series IDs or series; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool ParameterNameStartsWithSeries(ReadOnlySpan<char> setName)
	{
		return setName.StartsWith(
			['b', 'y', 's', 'e', 'r', 'i', 'e', 's'], StringComparison.OrdinalIgnoreCase);
	}
}
