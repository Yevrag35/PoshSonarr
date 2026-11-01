namespace MG.Sonarr.Next.Exceptions;

/// <summary>
/// Thrown when a buffer growth operation exceeds a configured or runtime-defined maximum buffer length.
/// </summary>
[DebuggerStepThrough]
public sealed class BufferLimitException : InvalidOperationException
{
	private const string DEF_MSG = "The buffer limit of '{0}' was exceeded. Previous length: {1}, Requested length: {2}.";

	/// <summary>
	/// Initializes a new instance of the <see cref="BufferLimitException"/> class with the specified buffer limit,
	/// previous length, and requested length.
	/// </summary>
	/// <param name="maxLength">The maximum allowed buffer length.</param>
	/// <param name="previousLength">The buffer length before the requested operation.</param>
	/// <param name="requestedLength">The requested buffer length that exceeds the maximum.</param>
	/// <param name="message">The message that describes the error.</param>
	public BufferLimitException(long maxLength, long previousLength, long requestedLength, string? message = null)
		: base(message ?? string.Format(CultureInfo.CurrentCulture, GetMessage(message), maxLength, previousLength, requestedLength))
	{
	}

	/// <summary>
	/// Gets the message template used to format the exception text.
	/// </summary>
	/// <param name="message">The custom message template.</param>
	/// <returns>The custom template when provided; otherwise, the default template.</returns>
	private static string GetMessage(string? message)
	{
		return !string.IsNullOrWhiteSpace(message) ? message : DEF_MSG;
	}
}
