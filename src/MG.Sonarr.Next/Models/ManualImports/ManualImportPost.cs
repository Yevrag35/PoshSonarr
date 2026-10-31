using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Models.Qualities;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.ManualImports;

/// <summary>
/// Represents one file in the body that a manual import posts.
/// </summary>
public sealed class ManualImportPost
{
	/// <summary>
	/// Gets or sets the path of the file to import.
	/// </summary>
	/// <value>The file's path on the server.</value>
	public required string Path { get; set; }
	/// <summary>
	/// Gets or sets the ID of the series that the file is imported into.
	/// </summary>
	/// <value>The series' ID.</value>
	public required int SeriesId { get; set; }
	/// <summary>
	/// Gets or sets the IDs of the episodes that the file is imported as.
	/// </summary>
	/// <value>The episodes' IDs.</value>
	public required int[] EpisodeIds { get; set; }
	/// <summary>
	/// Gets or sets the quality and revision that the file is imported with.
	/// </summary>
	/// <value>The quality's fields, keyed by name.</value>
	public required Dictionary<string, object?> Quality { get; set; }
	/// <summary>
	/// Gets or sets the languages of the file.
	/// </summary>
	/// <value>The fields of each language, keyed by name.</value>
	public required Dictionary<string, object?>[] Languages { get; set; }
	/// <summary>
	/// Gets or sets the release group of the file.
	/// </summary>
	/// <value>The release group, or an empty string for none.</value>
	public required string ReleaseGroup { get; set; }
	/// <summary>
	/// Gets or sets the indexer flags of the release that the file came from.
	/// </summary>
	/// <value>The flags, as Sonarr's number for them.</value>
	public required int IndexerFlags { get; set; }
	/// <summary>
	/// Gets or sets the kind of release that the file came from, such as a single episode or a season pack.
	/// </summary>
	/// <value>The release type, or an empty string for none.</value>
	public required string ReleaseType { get; set; }
}

/// <summary>
/// Provides the extension method that turns the files that Sonarr found for a manual import into the bodies that the
/// import posts.
/// </summary>
public static class ManualImportPostMapper
{
	/// <summary>
	/// Creates the body of each file in a list of manual import files.
	/// </summary>
	/// <remarks>
	/// Each body takes the file's path, series ID, episode IDs, quality, languages, release group, indexer flags, and
	/// release type, and leaves out the <c>MetadataTag</c> property of the quality and the languages. A file without a
	/// release group or release type gets an empty string, and one without languages gets an empty array.
	/// </remarks>
	/// <param name="list">The files to create bodies for.</param>
	/// <returns>An array with a body for each file in <paramref name="list"/>, in the same order.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="list"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when a file in <paramref name="list"/> has no path, no quality, or
	/// no series.</exception>
	public static ManualImportPost[] ToManualImportFiles(this MetadataList<ManualImportObject> list)
	{
		ArgumentNullException.ThrowIfNull(list);

		ManualImportPost[] array = new ManualImportPost[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			var obj = list[i];
			array[i] = new()
			{
				EpisodeIds = [.. obj.Episodes.Select(x => x.Id)],
				IndexerFlags = obj.GetValue<int>("IndexerFlags"),
				Languages = obj.GetValue<PSObject[]>(nameof(ManualImportPost.Languages))?.Select(x => x.ToDictionary(nameof(LanguageProfileObject.MetadataTag))).ToArray() ?? [],
				Path = obj.GetValue<string>(nameof(ManualImportPost.Path)) ?? throw new ArgumentException("Path cannot be null."),
				Quality = obj.Quality?.ToDictionary(nameof(QualityRevisionObject.MetadataTag)) ?? throw new ArgumentException("Quality cannot be null."),
				ReleaseGroup = obj.GetValue<string>(nameof(ManualImportPost.ReleaseGroup)) ?? string.Empty,
				ReleaseType = obj.GetValue<string>(nameof(ManualImportPost.ReleaseType)) ?? string.Empty,
				SeriesId = obj.Series?.Id ?? throw new ArgumentException("Series cannot be null."),
			};
		}

		return array;
	}
}

