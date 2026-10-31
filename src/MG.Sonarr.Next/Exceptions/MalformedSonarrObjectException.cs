namespace MG.Sonarr.Next.Exceptions;

/// <summary>
/// Represents an exception thrown when a Sonarr object has an unexpected or malformed shape.
/// </summary>
public sealed class MalformedSonarrObjectException : PoshSonarrException
{
	/// <summary>
	/// Gets the malformed object instance.
	/// </summary>
	/// <value>The malformed object, or <see langword="null"/> when unavailable.</value>
	public object? MalformedObject { get; }

	/// <summary>
	/// Initializes a new <see cref="MalformedSonarrObjectException"/> instance.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="malformedObject">The malformed object.</param>
	public MalformedSonarrObjectException(string? message, object? malformedObject)
		: this(message, malformedObject, innerException: null)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="MalformedSonarrObjectException"/> instance.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="malformedObject">The malformed object.</param>
	/// <param name="innerException">The exception that caused the current exception.</param>
	public MalformedSonarrObjectException(string? message, object? malformedObject, Exception? innerException)
		: base(message, innerException)
	{
		this.MalformedObject = malformedObject;
	}
}
