using MG.Sonarr.Next.Models.Episodes;

namespace MG.Sonarr.Next.Exceptions;

/// <summary>
/// Represents an exception that is thrown when one or more episode identifiers are invalid or malformed in the context of Sonarr.
/// </summary>
public sealed class InvalidEpisodeIdFormatException : PoshSonarrException
{
	const string ARG_MSG = "Episode identifiers should be either an integer or a string in \"S<season#>E<episode#>\" format.";

	/// <summary>
	/// Gets the list of offending episode identifiers that caused this exception to be thrown.
	/// </summary>
	public IReadOnlyList<IEpisodeIdentifier> OffendingIds { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="InvalidEpisodeIdFormatException"/> class with the specified offending identifiers and an optional inner exception.
	/// </summary>
	/// <param name="identifiers">The list of offending episode identifiers.</param>
	/// <param name="innerException">The exception that is the cause of the current exception.</param>
	public InvalidEpisodeIdFormatException(IReadOnlyList<IEpisodeIdentifier>? identifiers, Exception? innerException)
		: base(ARG_MSG, innerException)
	{
		this.OffendingIds = identifiers ?? [];
	}

	/// <summary>
	/// Creates a new instance of <see cref="InvalidEpisodeIdFormatException"/> from the given list of identifiers and an optional inner exception.
	/// </summary>
	/// <typeparam name="T">The type of the episode identifiers.</typeparam>
	/// <param name="identifiers">The list of offending episode identifiers.</param>
	/// <param name="innerException">The exception that is the cause of the current exception.</param>
	/// <returns>A new instance of <see cref="InvalidEpisodeIdFormatException"/>.</returns>
	public static InvalidEpisodeIdFormatException FromList<T>(IReadOnlyList<T>? identifiers, Exception? innerException)
		where T : IEpisodeIdentifier
	{
		if (identifiers is null || identifiers.Count <= 0)
		{
			return new InvalidEpisodeIdFormatException(null, innerException);
		}

		var array = new IEpisodeIdentifier[identifiers.Count];
		for (int i = 0; i < identifiers.Count; i++)
		{
			array[i] = new StoredId(identifiers[i]);
		}

		return new InvalidEpisodeIdFormatException(array, innerException);
	}

	private readonly struct StoredId : IEpisodeIdentifier
	{
		readonly EpisodeRange _ep;
		readonly int _season;
		readonly bool _isAb;
		readonly bool _isNotEmpty;

		public EpisodeRange EpisodeRange => _ep;
		public bool IsAbsolute => _isAb;
		internal bool IsEmpty => !_isNotEmpty;
		public int Season => _season;

		internal StoredId(IEpisodeIdentifier other)
		{
			_ep = other.EpisodeRange;
			_season = other.Season;
			_isNotEmpty = other.IsValid();
			_isAb = other.IsAbsolute;
		}

		public bool IsValid()
		{
			return _isNotEmpty;
		}
	}
}