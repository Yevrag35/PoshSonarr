using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Exceptions;

namespace MG.Sonarr.Next.Shell.Exceptions;

[Flags]
public enum ParameterErrorType
{
	None = 0,
	Invalid = 1,
	Missing = 2,
	Malformed = 4,
}

public sealed class SonarrParameterException : PoshSonarrException
{
	const string AND = "and ";
	const string MSG_FORMAT = "The cmdlet parameter '{0}' is {1}.";
	const string MSG_ADD_FORMAT = MSG_FORMAT + " {2}";

	public ParameterErrorType ErrorType { get; }
	public string ParameterName { get; }

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

		return string.Format(format, paramName, GetTypeString(in type), additionalMsg);
	}

	private static string GetTypeString(in ParameterErrorType type)
	{
		const int max_stackalloc = 256;

		string typeStr = type.ToString();
		ReadOnlySpan<char> typeSpan = typeStr;
		scoped ReadOnlySpan<char> splitBy = stackalloc char[] { ',', ' ' };
		int count = GetErrorCount(typeSpan, splitBy);

		int length = typeSpan.Length + 4;
		using var builder = SpanStringBuilder.Create(
			length <= max_stackalloc
				? stackalloc char[length]
				: length);

		if (count < 2)
		{
			typeSpan.Slice(0, 1).ToLower(builder.AppendSpan(1), Statics.DefaultCulture);
			builder.Append(typeSpan.Slice(1));
			return builder.ToString();
		}

		int i = 0;
		//foreach (ReadOnlySpan<char> section in typeSpan.SpanSplit(splitBy))
		foreach (Range range in typeSpan.Split(splitBy))
		{
			ReadOnlySpan<char> section = typeSpan[range];
			section.Slice(0, 1).ToLower(builder.AppendSpan(1), Statics.DefaultCulture);
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

	public SonarrErrorRecord ToRecord()
	{
		return this.ToRecord(this.ParameterName);
	}
	public SonarrErrorRecord ToRecord(object? targetObj)
	{
		return new SonarrErrorRecord(this, nameof(SonarrParameterException), ErrorCategory.InvalidArgument, targetObj);
	}
}
