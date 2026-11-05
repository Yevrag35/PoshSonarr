using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Attributes;

namespace MG.Sonarr.Next.Shell.Cmdlets.Series;

/// <summary>
/// Represents the <c>Remove-SonarrSeries</c> cmdlet, which deletes series from Sonarr's library, and with
/// <c>-DeleteFiles</c>, their files on disk too.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes the series' IDs through <c>-Id</c>, or series piped to it, and collects them until its input
/// ends, deleting each series once. Then it sends a delete request for each series that's confirmed, and writes
/// nothing to the pipeline. When Sonarr doesn't have a series, the cmdlet writes a warning, and it writes the error of
/// any other request that fails and goes on to the next series. When no series reaches it, it writes a warning.
/// </para>
/// <para>
/// Its confirm impact is high, so it asks <c>ShouldProcess</c> for each series, which prompts by default, and then
/// asks again with a prompt of its own that names the series. <c>-Force</c> skips both prompts, unless
/// <c>-Confirm</c> is given too, which brings back the first. <c>-WhatIf</c> shows what the cmdlet would delete and
/// deletes nothing, with or without <c>-Force</c>.
/// </para>
/// <para>
/// The cmdlet's alias is <c>Delete-SonarrSeries</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Remove, "SonarrSeries", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
[Alias("Delete-SonarrSeries")]
[MetadataCanPipe(Tag = Meta.Series)]
public sealed class RemoveSonarrSeriesCmdlet : SonarrApiCmdletBase
{
	const string DELETE_FILES = "deleteFiles";

	QueryCol _col = null!;
	SortedDictionary<int, string?> _dict = null!;
	/// <summary>
	/// Gets or sets the metadata tag of the series endpoint, which the cmdlet sends its delete requests to.
	/// </summary>
	/// <value>The <c>#series</c> tag, set when the cmdlet's scope is created.</value>
	MetadataTag Tag { get; set; } = null!;

	/// <summary>
	/// Gets or sets a value that indicates whether Sonarr also deletes the series' folders and files from disk.
	/// </summary>
	/// <value>The switch, which is present to delete the series' files.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = false)]
	public SwitchParameter DeleteFiles { get; set; }

	/// <summary>
	/// Gets or sets the IDs of the series to delete.
	/// </summary>
	/// <remarks>
	/// An ID given more than once binds once.
	/// </remarks>
	/// <value>The series' IDs, each greater than zero. The default is an empty array.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	[DistinctValues(typeof(int))]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets or sets the series to delete.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The parameter takes the series that <c>Get-SonarrSeries</c> writes from the pipeline, and each piped series
	/// arrives in an array of its own. Each series' ID has to be greater than zero, and the confirmation prompt names
	/// the series by its title.
	/// </para>
	/// <para>
	/// The parameter is hidden from tab completion, since it's meant for the pipeline.
	/// </para>
	/// </remarks>
	/// <value>The piped series. The default is an empty array.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline, DontShow = true)]
	[ValidateNotNull]
	[ValidateIds(ValidateRangeKind.Positive)]
	public SeriesObject[] InputObject { get; set; } = [];

	/// <summary>
	/// Gets or sets a value that indicates whether the cmdlet deletes the series without asking first.
	/// </summary>
	/// <remarks>
	/// The switch skips both confirmation prompts. With <c>-Confirm</c>, the cmdlet still prompts through
	/// <c>ShouldProcess</c>, and in What if mode it still deletes nothing.
	/// </remarks>
	/// <value>The switch, which is present to skip the confirmation prompts.</value>
	[Parameter(Mandatory = false)]
	public SwitchParameter Force { get; set; }

	/// <summary>
	/// Runs the base class's setup for the scope, then gets the metadata tag of the series endpoint and creates the
	/// collection of series IDs and the query that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		this.Tag = provider.GetRequiredService<IMetadataResolver>()[Meta.Series];

		_dict = [];
		_col = new(10);
	}

	/// <summary>
	/// Adds the ID and title of each of the specified series to the specified dictionary, unless it already holds the
	/// ID.
	/// </summary>
	/// <param name="array">The series to add.</param>
	/// <param name="dictionary">The series' titles, or <see langword="null"/>, by their IDs.</param>
	private static void AddIdsToDict(ReadOnlySpan<SeriesObject> array, SortedDictionary<int, string?> dictionary)
	{
		foreach (SeriesObject obj in array)
		{
			if (!dictionary.ContainsKey(obj.Id))
			{
				dictionary.Add(obj.Id, obj.Title);
			}
		}
	}
	/// <summary>
	/// Adds each of the specified IDs, without a title, to the specified dictionary, unless it already holds the ID.
	/// </summary>
	/// <param name="ids">The series' IDs.</param>
	/// <param name="dictionary">The series' titles, or <see langword="null"/>, by their IDs.</param>
	private static void AddIdsToDict(ReadOnlySpan<int> ids, SortedDictionary<int, string?> dictionary)
	{
		foreach (ref readonly int id in ids)
		{
			if (!dictionary.ContainsKey(id))
			{
				dictionary.Add(id, null);
			}
		}
	}

	/// <summary>
	/// Adds the IDs from <c>-Id</c>, and the IDs and titles of the piped series, to the series that the cmdlet deletes.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		if (this.Id.Length > 0)
		{
			AddIdsToDict(this.Id, _dict);
		}

		if (this.InputObject.Length > 0)
		{
			AddIdsToDict(this.InputObject, _dict);
		}
	}

	/// <summary>
	/// Sends a delete request for each series that's confirmed, in the order of the series' IDs.
	/// </summary>
	/// <remarks>
	/// When no series reached the cmdlet, it writes a warning and stops. Each request carries <c>deleteFiles=true</c>
	/// when <c>-DeleteFiles</c> is present.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		if (_dict.Count == 0)
		{
			this.WriteWarning("No series were passed via the pipeline. Make sure to pass the correct object type.");
			this.StopCmdlet();
			return;
		}

		_col.AddIfTrue(this.DeleteFiles, DELETE_FILES);
		bool force = this.Force.ToBool();
		bool noToAll = false;
		bool yesToAll = false;

		foreach (var kvp in _dict)
		{
			string url = GetUrl(this.Tag, kvp.Key, _col);

			if (this.ShouldProcessWithForce(url, "Deleting Series", force)
				&&
				(force
				||
				this.ShouldContinue(in kvp, ref yesToAll, ref noToAll)))
			{
				this.SendDeleteSeries(url);
			}
		}
	}

	/// <summary>
	/// Asks the user whether to delete the specified series, with a prompt that names it.
	/// </summary>
	/// <param name="kvp">The series' ID and its title, or <see langword="null"/> when the title isn't known.</param>
	/// <param name="yesToAll">
	/// <see langword="true"/> once the user has answered Yes to All, which skips the later prompts; otherwise,
	/// <see langword="false"/>.
	/// </param>
	/// <param name="noToAll">
	/// <see langword="true"/> once the user has answered No to All, which refuses the later series without a prompt;
	/// otherwise, <see langword="false"/>.
	/// </param>
	/// <returns><see langword="true"/> if the user agreed to delete the series; otherwise, <see langword="false"/>.</returns>
	private bool ShouldContinue(in KeyValuePair<int, string?> kvp, ref bool yesToAll, ref bool noToAll)
	{
		return this.ShouldContinue(
				query: GetQueryMessage(in kvp),
				caption: $"Delete Series ID: {kvp.Key}",
				yesToAll: ref yesToAll,
				noToAll: ref noToAll);
	}
	/// <summary>
	/// Sends the delete request for one series, and writes a warning or an error when it fails.
	/// </summary>
	/// <remarks>
	/// An ignorable error, as Sonarr's answer for a series that it doesn't have is, becomes a warning, and any other
	/// error is written to the error stream.
	/// </remarks>
	/// <param name="url">The series' endpoint path, with its query.</param>
	private void SendDeleteSeries(string url)
	{
		var result = this.SendDeleteRequest(url);
		if (result.IsError)
		{
			if (result.Error.IsIgnorable)
			{
				this.WriteWarning(result.Error.Message);
			}
			else
			{
				this.WriteConditionalError(result.Error);
			}
		}
	}
	/// <summary>
	/// Gets the question that the confirmation prompt asks for the specified series.
	/// </summary>
	/// <param name="kvp">The series' ID and its title, or <see langword="null"/> when the title isn't known.</param>
	/// <returns>The question, which names the series by its title and ID, or by its ID alone.</returns>
	private static string GetQueryMessage(in KeyValuePair<int, string?> kvp)
	{
		object id = !string.IsNullOrWhiteSpace(kvp.Value)
			? $"{kvp.Value} ({kvp.Key})"
			: kvp.Key;

		return $"Are you sure you want to delete the Series \"{id}\"?";
	}

	/// <summary>
	/// Gets the endpoint path of the series with the specified ID, with the specified query.
	/// </summary>
	/// <param name="tag">The metadata tag of the series endpoint.</param>
	/// <param name="id">The series' ID.</param>
	/// <param name="queryCollection">The query that the path carries.</param>
	/// <returns>The series' endpoint path, relative to the API.</returns>
	private static string GetUrl(MetadataTag tag, int id, QueryCol queryCollection)
	{
		return tag.GetUrlForId(id, queryCollection);
	}
}
