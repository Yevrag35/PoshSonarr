using MG.Sonarr.Next.Json.Converters;
using MG.Sonarr.Next.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Renames;

/// <summary>
/// Represents the body of the <c>RenameFiles</c> command, which renames episode files of one series.
/// </summary>
public sealed class PostRename
{
	/// <summary>
	/// The JSON name of the field that holds the file IDs.
	/// </summary>
	const string FILES = "files";
	/// <summary>
	/// The IDs of the episode files to rename.
	/// </summary>
	private readonly SortedSet<int> _ids = null!;

	/// <summary>
	/// Gets the IDs of the episode files to rename.
	/// </summary>
	/// <remarks>
	/// Setting the property copies the IDs into a sorted set, which drops duplicates, and a <see langword="null"/>
	/// value is ignored.
	/// </remarks>
	/// <value>The file IDs, in ascending order, which a request body writes as its <c>files</c> field.</value>
	[JsonPropertyName(FILES)]
	[JsonConverter(typeof(EnumerableSerializer<int>))]
	public required IEnumerable<int> FileIds
	{
		get => _ids;
		init
		{
			if (value is null)
			{
				return;
			}

			_ids = new(value);
		}
	}
	/// <summary>
	/// Gets the command's name.
	/// </summary>
	/// <value>Always <see cref="CommandStrings.RENAME_FILES"/>.</value>
	public string Name { get; } = CommandStrings.RENAME_FILES;
	/// <summary>
	/// Gets the ID of the series whose files are renamed.
	/// </summary>
	/// <value>The series' ID.</value>
	public required int SeriesId { get; init; }

	/// <summary>
	/// Initializes a new <see cref="PostRename"/> instance, whose required properties an object initializer sets.
	/// </summary>
	public PostRename()
	{
	}

	/// <summary>
	/// Initializes a new <see cref="PostRename"/> instance for one episode file of a series.
	/// </summary>
	/// <param name="seriesId">The ID of the series that the file belongs to.</param>
	/// <param name="episodeFileId">The ID of the episode file.</param>
	[SetsRequiredMembers]
	private PostRename(int seriesId, int episodeFileId)
	{
		this.FileIds = null!;
		_ids = new() { episodeFileId };
		this.SeriesId = seriesId;
	}

	/// <summary>
	/// Creates the bodies that rename a set of episode files, one for each series.
	/// </summary>
	/// <remarks>
	/// The method reads all of <paramref name="renames"/> before it returns, and a file that appears more than once
	/// is renamed once.
	/// </remarks>
	/// <param name="renames">The episode files to rename.</param>
	/// <returns>The bodies, in ascending order of series ID.</returns>
	public static IEnumerable<PostRename> FromRenameObjects(IEnumerable<IRenameFilePipeable> renames)
	{
		SortedDictionary<int, PostRename> dict = new();
		foreach (IRenameFilePipeable ro in renames)
		{
			if (dict.TryGetValue(ro.SeriesId, out PostRename? existing))
			{
				existing._ids.Add(ro.EpisodeFileId);
			}
			else
			{
				dict.Add(ro.SeriesId, new PostRename(ro.SeriesId, ro.EpisodeFileId));
			}
		}

		return dict.Values;
	}

	/// <summary>
	/// The format of <see cref="ToString"/>'s text, with the series ID and the comma-separated file IDs.
	/// </summary>
	const string FORMAT = "SeriesId = {0}; Ids = {{{1}}}";
	/// <summary>
	/// Returns a string that shows the series ID and the file IDs.
	/// </summary>
	/// <returns>Text such as <c>SeriesId = 5; Ids = {1,2,3}</c>, formatted with the invariant culture.</returns>
	public override string ToString()
	{
		string idsStr = string.Join(',', _ids);
		return string.Format(CultureInfo.InvariantCulture, FORMAT, this.SeriesId, idsStr);
	}
}
