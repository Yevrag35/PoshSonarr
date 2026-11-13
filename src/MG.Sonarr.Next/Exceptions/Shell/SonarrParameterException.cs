namespace MG.Sonarr.Next.Exceptions.Shell;

/// <summary>
/// Defines the types of errors that can occur with cmdlet parameters in Sonarr.
/// </summary>
[Flags]
public enum ParameterErrorType
{
	/// <summary>
	/// Indicates that there is no error with the parameter.
	/// </summary>
	None = 0,
	/// <summary>
	/// Indicates that the parameter is invalid.
	/// </summary>
	Invalid = 1,
	/// <summary>
	/// Indicates that the parameter is missing.
	/// </summary>
	Missing = 2,
	/// <summary>
	/// Indicates that the parameter is malformed.
	/// </summary>
	Malformed = 4,
}

/// <summary>
/// Represents an exception that is thrown when a cmdlet parameter is invalid, missing, or malformed in the context of Sonarr.
/// </summary>
public sealed class SonarrParameterException : PoshSonarrException
{
	private const string AND = "and ";
	private const string MSG_FORMAT = "The cmdlet parameter '{0}' is {1}.";
	private const string MSG_ADD_FORMAT = MSG_FORMAT + " {2}";

	/// <summary>
	/// Gets the type of error that occurred with the parameter.
	/// </summary>
	public ParameterErrorType ErrorType { get; }
	/// <summary>
	/// Gets the name of the parameter that caused the exception.
	/// </summary>
	public string ParameterName { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SonarrParameterException"/> class with the specified parameter name, error type, additional message, and inner exception.
	/// </summary>
	/// <param name="paramName">The name of the parameter that caused the exception.</param>
	/// <param name="errorType">The type of error that occurred with the parameter.</param>
	/// <param name="additionalMessage">An additional message that provides more details about the error.</param>
	/// <param name="innerException">The exception that is the cause of the current exception.</param>
	[ExcludeFromCodeCoverage]
	public SonarrParameterException(string paramName, ParameterErrorType errorType, string? additionalMessage, Exception? innerException = null)
		: base(GetMessage(paramName, errorType, additionalMessage), innerException)
	{
		this.ErrorType = errorType;
		this.ParameterName = paramName;
	}

	private static string GetMessage(string paramName, in ParameterErrorType type, string? additionalMsg)
	{
		string format = string.IsNullOrEmpty(additionalMsg)
			? MSG_FORMAT
			: MSG_ADD_FORMAT;

		return string.Format(CultureInfo.CurrentCulture, format, paramName, GetTypeString(in type), additionalMsg);
	}

	private static string GetTypeString(in ParameterErrorType type)
	{
		const int max_stackalloc = 256;

		string typeStr = type.ToString();
		ReadOnlySpan<char> typeSpan = typeStr;
		scoped ReadOnlySpan<char> splitBy = [',', ' '];
		int count = GetErrorCount(typeSpan, splitBy);

		int length = typeSpan.Length + 4;
		using var builder = SpanStringBuilder.Create(
			length <= max_stackalloc
				? stackalloc char[length]
				: length);

		if (count < 2)
		{
			typeSpan.Slice(0, 1).ToLower(builder.AppendSpan(1), CultureInfo.InvariantCulture);
			builder.Append(typeSpan.Slice(1));
			return builder.ToString();
		}

		int i = 0;
		foreach (Range range in typeSpan.Split(splitBy))
		{
			ReadOnlySpan<char> section = typeSpan[range];
			section.Slice(0, 1).ToLower(builder.AppendSpan(1), CultureInfo.InvariantCulture);
			builder.Append(section.Slice(1));

			if (i < count - 1)
			{
				if (count > 2)
				{
					builder.Append(splitBy);
				}
				else
				{
					builder.Append(splitBy.Slice(1));
				}
			}

			i++;

			if (i == count - 1)
			{
				builder.Append(AND);
			}
		}

		return builder.ToString();
	}

	private static int GetErrorCount(ReadOnlySpan<char> typeSpan, ReadOnlySpan<char> splitBy)
	{
		return typeSpan.Count(splitBy) + 1;
	}

	/// <summary>
	/// Creates an <see cref="ErrorRecord"/> representing this exception, with the parameter name as the target object.
	/// </summary>
	/// <returns>An <see cref="ErrorRecord"/> representing this exception.</returns>
	public SonarrErrorRecord ToRecord()
	{
		return this.ToRecord(this.ParameterName);
	}
	/// <summary>
	/// Creates an <see cref="ErrorRecord"/> representing this exception, with the specified target object.
	/// </summary>
	/// <param name="targetObj">The target object to associate with the error record.</param>
	/// <returns>An <see cref="ErrorRecord"/> representing this exception.</returns>
	public SonarrErrorRecord ToRecord(object? targetObj)
	{
		return new SonarrErrorRecord(this, nameof(SonarrParameterException), ErrorCategory.InvalidArgument, targetObj);
	}
}
