namespace MG.Sonarr.Next.Exceptions;

/// <summary>
/// Represents an exception thrown during module startup configuration.
/// </summary>
[DebuggerStepThrough]
public sealed class ModuleStartupException : Exception
{
	/// <summary>
	/// Gets the startup type associated with the failure.
	/// </summary>
	/// <value>The offending startup type, or <see langword="null"/> when unavailable.</value>
	public Type? OffendingStartupType { get; }

	/// <summary>
	/// Initializes a new <see cref="ModuleStartupException"/> instance.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public ModuleStartupException(string? message)
		: this(message, offendingType: null, innerException: null)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="ModuleStartupException"/> instance.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="offendingType">The startup type associated with the failure.</param>
	public ModuleStartupException(string? message, Type? offendingType)
		: this(message, offendingType, innerException: null)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="ModuleStartupException"/> instance.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="innerException">The exception that caused the current exception.</param>
	public ModuleStartupException(string? message, Exception? innerException)
		: this(message, offendingType: null, innerException)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="ModuleStartupException"/> instance.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="offendingType">The startup type associated with the failure.</param>
	/// <param name="innerException">The exception that caused the current exception.</param>
	public ModuleStartupException(string? message, Type? offendingType, Exception? innerException)
		: base(message, innerException)
	{
		this.OffendingStartupType = offendingType;
	}
}
