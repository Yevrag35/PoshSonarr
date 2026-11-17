using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.ManualImports;
using MG.Sonarr.Next.Models.Qualities;
using MG.Sonarr.Next.Models.Series;

namespace MG.Sonarr.Next.Shell.Components;

/// <summary>
/// Represents the changes that <c>Edit-SonarrManualImport</c> makes to each manual import piped to it.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet's DI scope holds one instance for the length of its run. The cmdlet's <c>Begin</c> sets the episode,
/// quality, and series from <c>-Episode</c>, <c>-Quality</c>, and <c>-Series</c>, and its <c>Process</c> applies them
/// to each import with <see cref="EditImport(ManualImportObject)"/>. A change whose object couldn't be found stays
/// unset, and the edit leaves that part of each import as it is.
/// </para>
/// <para>
/// Instances aren't thread-safe.
/// </para>
/// </remarks>
internal sealed class ManualImportEdit
{
	/// <summary>
	/// Gets a value that indicates whether the edit has an episode to add to each import.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if <see cref="Episode"/> isn't <see langword="null"/>; otherwise, <see langword="false"/>.
	/// </value>
	[MemberNotNullWhen(true, nameof(Episode))]
	internal bool HasEpisode { get; private set; }

	/// <summary>
	/// Gets a value that indicates whether the edit has a quality to give each import.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if <see cref="Quality"/> isn't <see langword="null"/>; otherwise, <see langword="false"/>.
	/// </value>
	[MemberNotNullWhen(true, nameof(Quality))]
	internal bool HasQuality { get; private set; }

	/// <summary>
	/// Gets a value that indicates whether the edit has a series to give each import.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if <see cref="Series"/> isn't <see langword="null"/>; otherwise, <see langword="false"/>.
	/// </value>
	[MemberNotNullWhen(true, nameof(Series))]
	internal bool HasSeries { get; private set; }

	/// <summary>
	/// Gets or sets the episode that the edit adds to each import's episodes.
	/// </summary>
	/// <remarks>
	/// Setting the property also sets <see cref="HasEpisode"/>.
	/// </remarks>
	/// <value>The episode to add, or <see langword="null"/> to add none. The default is <see langword="null"/>.</value>
	internal EpisodeObject? Episode
	{
		get;
		set
		{
			field = value;
			this.HasEpisode = value is not null;
		}
	}
	/// <summary>
	/// Gets or sets the quality and revision that the edit gives each import.
	/// </summary>
	/// <remarks>
	/// Setting the property also sets <see cref="HasQuality"/>.
	/// </remarks>
	/// <value>
	/// The quality that replaces each import's own, or <see langword="null"/> to leave it as it is. The default is
	/// <see langword="null"/>.
	/// </value>
	internal QualityRevisionObject? Quality
	{
		get;
		set
		{
			field = value;
			this.HasQuality = value is not null;
		}
	}
	/// <summary>
	/// Gets or sets the series that the edit gives each import.
	/// </summary>
	/// <remarks>
	/// Setting the property also sets <see cref="HasSeries"/>.
	/// </remarks>
	/// <value>
	/// The series that replaces each import's own, or <see langword="null"/> to leave it as it is. The default is
	/// <see langword="null"/>.
	/// </value>
	internal SeriesObject? Series
	{
		get;
		set
		{
			field = value;
			this.HasSeries = value is not null;
		}
	}

	/// <summary>
	/// Initializes a new <see cref="ManualImportEdit"/> instance that makes no changes.
	/// </summary>
	public ManualImportEdit()
	{
	}

	/// <summary>
	/// Applies the edit to the specified import: adds the episode to its episodes, and replaces its quality and its
	/// series, for each of them that the edit has.
	/// </summary>
	/// <remarks>
	/// The import keeps the episodes that it already has.
	/// </remarks>
	/// <param name="importObj">The manual import to change.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="importObj"/> is null.</exception>
	internal void EditImport(ManualImportObject importObj)
	{
		ArgumentNullException.ThrowIfNull(importObj);

		if (this.HasEpisode)
		{
			importObj.Episodes.Add(this.Episode);
		}

		if (this.HasQuality)
		{
			importObj.Quality = this.Quality;
		}

		if (this.HasSeries)
		{
			importObj.Series = this.Series;
		}
	}
}

