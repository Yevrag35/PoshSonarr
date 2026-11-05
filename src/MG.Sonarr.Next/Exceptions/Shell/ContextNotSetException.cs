namespace MG.Sonarr.Next.Exceptions.Shell;

/// <summary>
/// Represents an exception that is thrown when the Sonarr context is not set, indicating that a connection to a Sonarr instance has not been established.
/// </summary>
public sealed class ContextNotSetException : PoshSonarrException
{
	private const string DEF_MSG = "The Sonarr context is not set. First run 'Connect-SonarrInstance' to connect to an instance.";

	/// <summary>
	/// Initializes a new instance of <see cref="ContextNotSetException"/> with the default message and an optional inner exception.
	/// </summary>
	/// <param name="innerException">The exception that is the cause of the current exception.</param>
	public ContextNotSetException(Exception? innerException)
		: base(DEF_MSG, innerException)
	{
	}
}
