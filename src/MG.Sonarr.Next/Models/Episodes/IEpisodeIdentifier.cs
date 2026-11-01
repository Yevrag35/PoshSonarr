namespace MG.Sonarr.Next.Models.Episodes;

/// <summary>
/// Represents the episodes that a season and an episode range name, or that an absolute range names.
/// </summary>
public interface IEpisodeIdentifier
{
	/// <summary>
	/// Gets the range of episode numbers.
	/// </summary>
	/// <value>
	/// The range of absolute episode numbers when <see cref="IsAbsolute"/> is <see langword="true"/>, or else the
	/// range of episode numbers in <see cref="Season"/>.
	/// </value>
	EpisodeRange EpisodeRange { get; }
	/// <summary>
	/// Gets a value that indicates whether <see cref="EpisodeRange"/> holds absolute episode numbers.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the range holds absolute episode numbers; otherwise, <see langword="false"/>.
	/// </value>
	bool IsAbsolute { get; }
	/// <summary>
	/// Gets the season number.
	/// </summary>
	/// <value>The number of the season that <see cref="EpisodeRange"/> is in, when it isn't absolute.</value>
	int Season { get; }

	/// <summary>
	/// Determines whether the identifier names a valid set of episodes.
	/// </summary>
	/// <returns><see langword="true"/> if the identifier is valid; otherwise, <see langword="false"/>.</returns>
	bool IsValid();
}

