namespace MG.Sonarr.Next.Exceptions;

/// <summary>
/// Represents the base exception type for exceptions thrown by PoshSonarr libraries.
/// </summary>
public class PoshSonarrException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PoshSonarrException"/> class with a specified
	/// error message.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	[ExcludeFromCodeCoverage]
	protected PoshSonarrException(string? message)
		: base(message)
	{
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="PoshSonarrException"/> class with a specified error
	/// message and a reference to an inner exception that is the cause of this exception.
	/// </summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerException">
	///     The exception that is the casue of the current exception, or a <see langword="null"/> reference
	///     if no inner exception is specified.
	/// </param>
	[ExcludeFromCodeCoverage]
	protected PoshSonarrException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	/// <summary>
	/// Trims leading "this." from the specified <paramref name="argumentName"/> if it is present.
	/// </summary>
	/// <param name="argumentName">The argument name to trim.</param>
	/// <returns>
	/// The trimmed <paramref name="argumentName"/> if it starts with "this."; otherwise, the original string unchanged.
	/// </returns>
	[return: NotNullIfNotNull(nameof(argumentName))]
	protected static string? TrimThisDot(string? argumentName)
	{
		const string this_dot = "this.";

		if (string.IsNullOrWhiteSpace(argumentName)
			|| argumentName.Length <= this_dot.Length
			|| !argumentName.StartsWith(this_dot, StringComparison.Ordinal))
		{
			return argumentName;
		}

		return new(argumentName.AsSpan(this_dot.Length).Trim());
	}
}
