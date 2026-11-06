using MG.Sonarr.Next.Models.Episodes;

namespace MG.Sonarr.Next.Exceptions;

/// <summary>
/// Represents the exception that is thrown when one or more episode identifiers aren't valid.
/// </summary>
/// <remarks>
/// The message names each identifier that isn't valid, in quotes, by the text that its <see cref="object.ToString"/>
/// returns, and then says what a valid identifier is.
/// </remarks>
public sealed class InvalidEpisodeIdFormatException : PoshSonarrException
{
	/// <summary>
	/// What a valid identifier is, which ends every message.
	/// </summary>
	const string FORMS = "An identifier is S and a season number for the whole season, as in S01; that, E, and an episode number or a range of them, as in S01E02, S01E02-05, or S01E02-E05; or an absolute episode number or range, as in 25, E25, or E25-28. A number can't be negative, and a range can't end before it starts.";

	/// <summary>
	/// Gets the identifiers that aren't valid.
	/// </summary>
	/// <value>The identifiers that aren't valid, or an empty list when none was given.</value>
	public IReadOnlyList<IEpisodeIdentifier> OffendingIds { get; }

	/// <summary>
	/// Initializes a new <see cref="InvalidEpisodeIdFormatException"/> instance for the identifiers that aren't valid,
	/// and the exception that caused it, if any.
	/// </summary>
	/// <param name="identifiers">The identifiers that aren't valid, or <see langword="null"/> for none.</param>
	/// <param name="innerException">
	/// The exception that is the cause of this one, or <see langword="null"/> when there is none.
	/// </param>
	public InvalidEpisodeIdFormatException(IReadOnlyList<IEpisodeIdentifier>? identifiers, Exception? innerException)
		: base(GetMessage(identifiers), innerException)
	{
		this.OffendingIds = identifiers ?? [];
	}

	/// <summary>
	/// Creates an <see cref="InvalidEpisodeIdFormatException"/> for the identifiers that aren't valid, from a copy of
	/// each one, and the exception that caused it, if any.
	/// </summary>
	/// <remarks>
	/// Each copy keeps the identifier's range, its season, whether it's absolute, whether it's valid, and its text, so
	/// the exception doesn't hold the identifiers that it was given.
	/// </remarks>
	/// <typeparam name="T">The type of the episode identifiers.</typeparam>
	/// <param name="identifiers">The identifiers that aren't valid, or <see langword="null"/> for none.</param>
	/// <param name="innerException">
	/// The exception that is the cause of this one, or <see langword="null"/> when there is none.
	/// </param>
	/// <returns>A new <see cref="InvalidEpisodeIdFormatException"/> that names the identifiers.</returns>
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

	/// <summary>
	/// Builds the message that names the identifiers that aren't valid and says what a valid identifier is.
	/// </summary>
	/// <param name="identifiers">The identifiers that aren't valid, or <see langword="null"/> for none.</param>
	/// <returns>
	/// A message such as <c>The episode identifiers 'S01Ex' and 'S01E05-03' aren't valid.</c>, followed by the forms
	/// that an identifier takes, or one that starts <c>An episode identifier isn't valid.</c> when
	/// <paramref name="identifiers"/> is <see langword="null"/> or empty.
	/// </returns>
	private static string GetMessage(IReadOnlyList<IEpisodeIdentifier>? identifiers)
	{
		const string NONE = "An episode identifier isn't valid. ";
		const string ONE = "The episode identifier ";
		const string SEVERAL = "The episode identifiers ";
		const string IS_NOT_VALID = " isn't valid. ";
		const string ARE_NOT_VALID = " aren't valid. ";

		int count = identifiers?.Count ?? 0;
		if (count == 0)
		{
			return NONE + FORMS;
		}

		using SpanStringBuilder builder = new(FORMS.Length + 64 + (count * 16));
		builder.Append(count == 1 ? ONE : SEVERAL);
		for (int i = 0; i < count; i++)
		{
			if (i > 0)
			{
				builder.Append(count == 2 ? " and " : i == count - 1 ? ", and " : ", ");
			}

			builder.Append('\'');
			builder.Append(identifiers![i].ToString());
			builder.Append('\'');
		}

		builder.Append(count == 1 ? IS_NOT_VALID : ARE_NOT_VALID);
		builder.Append(FORMS);
		return builder.ToString();
	}

	/// <summary>
	/// Represents a copy of an episode identifier that isn't valid.
	/// </summary>
	private readonly struct StoredId : IEpisodeIdentifier
	{
		readonly EpisodeRange _ep;
		readonly int _season;
		readonly bool _isAb;
		readonly bool _isValid;
		readonly string _text;

		/// <summary>
		/// Gets the range of episode numbers.
		/// </summary>
		/// <value>The range of the identifier that was copied.</value>
		public EpisodeRange EpisodeRange => _ep;
		/// <summary>
		/// Gets a value that indicates whether <see cref="EpisodeRange"/> holds absolute episode numbers.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the range holds absolute episode numbers; otherwise, <see langword="false"/>.
		/// </value>
		public bool IsAbsolute => _isAb;
		/// <summary>
		/// Gets the season number.
		/// </summary>
		/// <value>The season number of the identifier that was copied.</value>
		public int Season => _season;

		/// <summary>
		/// Initializes a new <see cref="StoredId"/> instance from the identifier to copy.
		/// </summary>
		/// <param name="other">The identifier to copy.</param>
		internal StoredId(IEpisodeIdentifier other)
		{
			_ep = other.EpisodeRange;
			_season = other.Season;
			_isValid = other.IsValid();
			_isAb = other.IsAbsolute;
			_text = other.ToString() ?? string.Empty;
		}

		/// <summary>
		/// Determines whether the identifier that was copied was valid.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the identifier that was copied was valid; otherwise, <see langword="false"/>.
		/// </returns>
		public bool IsValid()
		{
			return _isValid;
		}

		/// <summary>
		/// Returns the text of the identifier that was copied.
		/// </summary>
		/// <returns>The text that the identifier's <see cref="object.ToString"/> returned.</returns>
		public override string ToString()
		{
			return _text;
		}
	}
}
