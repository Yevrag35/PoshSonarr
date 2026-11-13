namespace MG.Sonarr.Next.Exceptions.Shell;

/// <summary>
/// An exception thrown when a blank, <see langword="null"/>, or invalid API key is supplied during 
/// the initial connection to the Sonarr server.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class InvalidApiKeyException : PoshSonarrException
{
	private const string DEF_MSG = "The supplied Sonarr API key is null, blank, or invalid.";

	/// <summary>
	/// Initializes a new instance of <see cref="InvalidApiKeyException"/> with the default message.
	/// </summary>
	public InvalidApiKeyException()
		: base(DEF_MSG)
	{
	}
}
