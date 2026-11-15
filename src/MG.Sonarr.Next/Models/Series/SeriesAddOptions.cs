namespace MG.Sonarr.Next.Models.Series;

/// <summary>
/// Represents the options that Sonarr applies when it adds a series to the library.
/// </summary>
[DebuggerStepThrough]
public abstract record SeriesAddOptions
{
	/// <summary>
	/// When overridden in a derived class, gets a value that indicates whether Sonarr leaves unmonitored the
	/// episodes that already have files.
	/// </summary>
	/// <value>
	///     <see langword="true"/> if episodes with files are left unmonitored; otherwise, <see langword="false"/>.
	/// </value>
	public abstract bool IgnoreEpisodesWithFiles { get; }
	/// <summary>
	/// When overridden in a derived class, gets a value that indicates whether Sonarr leaves unmonitored the
	/// episodes that don't have files yet.
	/// </summary>
	/// <value>
	///     <see langword="true"/> if episodes without files are left unmonitored; otherwise,
	///     <see langword="false"/>.
	/// </value>
	public abstract bool IgnoreEpisodesWithoutFiles { get; }
	/// <summary>
	/// When overridden in a derived class, gets a value that indicates whether Sonarr searches for the series'
	/// missing episodes as soon as it adds the series.
	/// </summary>
	/// <value>
	///     <see langword="true"/> if Sonarr searches for missing episodes after adding the series; otherwise,
	///     <see langword="false"/>.
	/// </value>
	public abstract bool SearchForMissingEpisodes { get; }

	/// <summary>
	/// The default options, which ignore episodes with and without files and don't search for missing episodes.
	/// </summary>
	public static readonly SeriesAddOptions Default = new DefaultInstance();

	private sealed record DefaultInstance : SeriesAddOptions
	{
		public override bool IgnoreEpisodesWithFiles => true;
		public override bool IgnoreEpisodesWithoutFiles => true;
		public override bool SearchForMissingEpisodes => false;
	}
}
