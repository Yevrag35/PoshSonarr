using MG.Sonarr.Next.Models.Series;

namespace MG.Sonarr.Next.Shell.Internal;

/// <summary>
/// Represents a value of <c>Add-SonarrSeries</c>'s <c>-SearchForMissingEpisodes</c>, which has Sonarr search for the
/// series' missing episodes once it adds the series, and says which episodes Sonarr leaves unmonitored.
/// </summary>
/// <remarks>
/// Any value given has Sonarr search for missing episodes. Without the parameter, Sonarr doesn't search, and leaves
/// unmonitored the episodes with files and those without, as <see cref="SeriesAddOptions.Default"/> does. A value given
/// more than once binds once.
/// </remarks>
public enum SeriesAddIgnoreAction
{
	/// <summary>Sonarr searches for missing episodes and ignores no episodes.</summary>
	Default,
	/// <summary>
	/// Sonarr searches for missing episodes and leaves unmonitored the episodes that already have files.
	/// </summary>
	IgnoreWithEpisodes,
	/// <summary>
	/// Sonarr searches for missing episodes and leaves unmonitored the episodes that don't have files yet.
	/// </summary>
	IgnoreWithoutEpisodes,
}

/// <summary>
/// Represents the add options, whose values can be set, that <c>Add-SonarrSeries</c> builds from
/// <c>-SearchForMissingEpisodes</c>.
/// </summary>
/// <remarks>
/// Until its properties are set, the options match <see cref="SeriesAddOptions.Default"/>: Sonarr leaves unmonitored
/// the episodes with files and those without, and doesn't search. Equality is the record's own, which compares the
/// three settable properties.
/// </remarks>
[DebuggerStepThrough]
internal sealed record EditableSeriesAddOptions : SeriesAddOptions
{
	/// <summary>
	/// Gets a value that indicates whether Sonarr leaves unmonitored the episodes that already have files.
	/// </summary>
	/// <value>The value of <see cref="IgnoreEpsWithFiles"/>.</value>
	public override bool IgnoreEpisodesWithFiles => this.IgnoreEpsWithFiles;
	/// <summary>
	/// Gets a value that indicates whether Sonarr leaves unmonitored the episodes that don't have files yet.
	/// </summary>
	/// <value>The value of <see cref="IgnoreEpsWithoutFiles"/>.</value>
	public override bool IgnoreEpisodesWithoutFiles => this.IgnoreEpsWithoutFiles;
	/// <summary>
	/// Gets a value that indicates whether Sonarr searches for the series' missing episodes as soon as it adds the
	/// series.
	/// </summary>
	/// <value>The value of <see cref="SearchForMissingEps"/>.</value>
	public override bool SearchForMissingEpisodes => this.SearchForMissingEps;

	/// <summary>
	/// Gets or sets a value that indicates whether Sonarr leaves unmonitored the episodes that already have files.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if episodes with files are left unmonitored; otherwise, <see langword="false"/>. The
	/// default is <see langword="true"/>.
	/// </value>
	internal bool IgnoreEpsWithFiles { get; set; } = true;
	/// <summary>
	/// Gets or sets a value that indicates whether Sonarr leaves unmonitored the episodes that don't have files yet.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if episodes without files are left unmonitored; otherwise, <see langword="false"/>. The
	/// default is <see langword="true"/>.
	/// </value>
	internal bool IgnoreEpsWithoutFiles { get; set; } = true;
	/// <summary>
	/// Gets or sets a value that indicates whether Sonarr searches for the series' missing episodes as soon as it adds
	/// the series.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if Sonarr searches for missing episodes; otherwise, <see langword="false"/>. The default is
	/// <see langword="false"/>.
	/// </value>
	internal bool SearchForMissingEps { get; set; }
}
