using MG.Sonarr.Next.Extensions.Reflection;

namespace MG.Sonarr.Next.Exceptions;

/// <summary>
/// Represents a base exception for mismatched expected and actual value types.
/// </summary>
public class InvalidTypeException : PoshSonarrException
{
	/// <summary>
	/// Gets the actual runtime type.
	/// </summary>
	/// <value>The actual runtime type, or <see langword="null"/> when the value is null.</value>
	public Type? ActualType { get; }

	/// <summary>
	/// Gets the expected type.
	/// </summary>
	/// <value>The expected type.</value>
	public Type ExpectedType { get; }

	/// <summary>
	/// Initializes a new <see cref="InvalidTypeException"/> instance.
	/// </summary>
	/// <param name="msgFormat">The message format string.</param>
	/// <param name="expectedType">The expected type.</param>
	/// <param name="actualValue">The actual value to evaluate.</param>
	/// <param name="innerException">The exception that caused the current exception.</param>
	protected InvalidTypeException(string msgFormat, Type expectedType, object? actualValue, Exception? innerException)
		: base(GetMessage(msgFormat, ref expectedType, actualValue, out Type? actualType), innerException)
	{
		this.ActualType = actualType;
		this.ExpectedType = expectedType;
	}

	/// <summary>
	/// Builds the formatted mismatch message.
	/// </summary>
	/// <param name="msgFormat">The message format string.</param>
	/// <param name="keyType">The expected type.</param>
	/// <param name="key">The actual value.</param>
	/// <param name="actualType">The resolved runtime type of <paramref name="key"/>.</param>
	/// <returns>The formatted message text.</returns>
	private static string GetMessage(string msgFormat, ref Type keyType, object? key, out Type? actualType)
	{
		keyType ??= typeof(object);
		actualType = key?.GetType();

		string actual = actualType?.GetName() ?? "null";

		return string.Format(CultureInfo.CurrentCulture, msgFormat, keyType.GetName(), actual);
	}
}

/// <summary>
/// Represents an exception thrown when a dictionary key has an unexpected type.
/// </summary>
public sealed class InvalidKeyTypeException : InvalidTypeException
{
	const string DEF_MSG = "The dictionary key was not of the correct type - expected '{0}' and got '{1}'.";

	/// <summary>
	/// Initializes a new <see cref="InvalidKeyTypeException"/> instance.
	/// </summary>
	/// <param name="keyType">The expected key type.</param>
	/// <param name="key">The key value that failed type validation.</param>
	public InvalidKeyTypeException(Type keyType, object? key)
		: this(keyType, key, null)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="InvalidKeyTypeException"/> instance.
	/// </summary>
	/// <param name="keyType">The expected key type.</param>
	/// <param name="key">The key value that failed type validation.</param>
	/// <param name="innerException">The exception that caused the current exception.</param>
	public InvalidKeyTypeException(Type keyType, object? key, Exception? innerException)
		: base(DEF_MSG, keyType, key, innerException)
	{
	}
}

/// <summary>
/// Represents an exception thrown when a dictionary value has an unexpected type.
/// </summary>
public sealed class InvalidValueTypeException : InvalidTypeException
{
	const string DEF_MSG = "The dictionary value was not of the correct type - expected '{0}' and got '{1}'.";

	/// <summary>
	/// Initializes a new <see cref="InvalidValueTypeException"/> instance.
	/// </summary>
	/// <param name="valueType">The expected value type.</param>
	/// <param name="value">The value that failed type validation.</param>
	public InvalidValueTypeException(Type valueType, object? value)
		: this(valueType, value, null)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="InvalidValueTypeException"/> instance.
	/// </summary>
	/// <param name="valueType">The expected value type.</param>
	/// <param name="value">The value that failed type validation.</param>
	/// <param name="innerException">The exception that caused the current exception.</param>
	public InvalidValueTypeException(Type valueType, object? value, Exception? innerException)
		: base(DEF_MSG, valueType, value, innerException)
	{
	}
}

