using MG.Sonarr.Next.Extensions.Reflection;
using System.Management.Automation;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Exception"/>.
/// </summary>
public static class ExceptionExtensions
{
	/// <summary>
	/// Gets the name of an exception's type, for display or logging.
	/// </summary>
	/// <param name="exception">The exception whose type name is returned, or <see langword="null"/>.</param>
	/// <returns>
	/// The type's <see cref="Type.FullName"/>, or its <see cref="MemberInfo.Name"/> when it has no full name, as
	/// <see cref="TypeExtensions.GetName(Type)"/> returns it; or <see langword="null"/> when
	/// <paramref name="exception"/> is <see langword="null"/>.
	/// </returns>
	[return: NotNullIfNotNull(nameof(exception))]
	public static string? GetTypeName(this Exception? exception)
	{
		return exception?.GetType() is Type eType ? eType.GetName() : null;
	}

	/// <summary>
	/// Creates a PowerShell <see cref="ErrorRecord"/> for an exception, with the category
	/// <see cref="ErrorCategory.NotSpecified"/> and no target object.
	/// </summary>
	/// <remarks>
	/// The record's error ID is the exception's type name, as <see cref="GetTypeName(Exception)"/> returns it.
	/// </remarks>
	/// <param name="exception">The exception that describes the error, or <see langword="null"/>.</param>
	/// <returns>
	/// A new <see cref="ErrorRecord"/> for <paramref name="exception"/>, or <see langword="null"/> when
	/// <paramref name="exception"/> is <see langword="null"/>.
	/// </returns>
	[return: NotNullIfNotNull(nameof(exception))]
	public static ErrorRecord? ToRecord(this Exception? exception)
	{
		return ToRecord(exception, ErrorCategory.NotSpecified, targetObj: null);
	}

	/// <summary>
	/// Creates a PowerShell <see cref="ErrorRecord"/> for an exception, with the specified category and no target
	/// object.
	/// </summary>
	/// <remarks>
	/// The record's error ID is the exception's type name, as <see cref="GetTypeName(Exception)"/> returns it.
	/// </remarks>
	/// <param name="exception">The exception that describes the error, or <see langword="null"/>.</param>
	/// <param name="category">The category that best describes the error.</param>
	/// <returns>
	/// A new <see cref="ErrorRecord"/> for <paramref name="exception"/>, or <see langword="null"/> when
	/// <paramref name="exception"/> is <see langword="null"/>.
	/// </returns>
	[return: NotNullIfNotNull(nameof(exception))]
	public static ErrorRecord? ToRecord(this Exception? exception, ErrorCategory category)
	{
		return ToRecord(exception, category, targetObj: null);
	}

	/// <summary>
	/// Creates a PowerShell <see cref="ErrorRecord"/> for an exception, with the specified category and target
	/// object.
	/// </summary>
	/// <remarks>
	/// The record's error ID is the exception's type name, as <see cref="GetTypeName(Exception)"/> returns it.
	/// </remarks>
	/// <param name="exception">The exception that describes the error, or <see langword="null"/>.</param>
	/// <param name="category">The category that best describes the error.</param>
	/// <param name="targetObj">
	/// The object that the cmdlet was acting on when the error occurred, or <see langword="null"/> for none.
	/// </param>
	/// <returns>
	/// A new <see cref="ErrorRecord"/> for <paramref name="exception"/>, or <see langword="null"/> when
	/// <paramref name="exception"/> is <see langword="null"/>.
	/// </returns>
	[return: NotNullIfNotNull(nameof(exception))]
	public static ErrorRecord? ToRecord(this Exception? exception, ErrorCategory category, object? targetObj)
	{
		if (exception is null)
		{
			return null;
		}

		return new ErrorRecord(
			exception: exception,
			errorId: exception.GetTypeName(),
			errorCategory: category,
			targetObject: targetObj);
	}
}
