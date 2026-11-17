using MG.Sonarr.Next.Extensions.Reflection;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Exception"/>.
/// </summary>
internal static class ExceptionExtensions
{
	/// <summary>
	/// Provides extension methods that name an <see cref="Exception"/> that may be <see langword="null"/>, or
	/// create a PowerShell <see cref="ErrorRecord"/> for it.
	/// </summary>
	/// <param name="exception">The exception that describes the error, or <see langword="null"/>.</param>
	extension(Exception? exception)
	{
		/// <summary>
		/// Gets the name of an exception's type, for display or logging.
		/// </summary>
		/// <returns>
		/// The type's <see cref="Type.FullName"/>, or its <see cref="MemberInfo.Name"/> when it has no full name, as
		/// <see cref="MG.Sonarr.Next.Extensions.Reflection.TypeExtensions.extension(Type).GetName()"/> returns it; or
		/// <see langword="null"/> when <paramref name="exception"/> is <see langword="null"/>.
		/// </returns>
		[return: NotNullIfNotNull(nameof(exception))]
		internal string? GetTypeName()
		{
			return exception?.GetType() is Type eType ? eType.GetName() : null;
		}

		/// <summary>
		/// Creates a PowerShell <see cref="ErrorRecord"/> for an exception, with the category
		/// <see cref="ErrorCategory.NotSpecified"/> and no target object.
		/// </summary>
		/// <remarks>
		/// The record's error ID is the exception's type name, as <see cref="extension(Exception?).GetTypeName()"/>
		/// returns it.
		/// </remarks>
		/// <returns>
		/// A new <see cref="ErrorRecord"/> for <paramref name="exception"/>, or <see langword="null"/> when
		/// <paramref name="exception"/> is <see langword="null"/>.
		/// </returns>
		[return: NotNullIfNotNull(nameof(exception))]
		internal ErrorRecord? ToRecord()
		{
			return exception.ToRecord(ErrorCategory.NotSpecified, targetObj: null);
		}

		/// <summary>
		/// Creates a PowerShell <see cref="ErrorRecord"/> for an exception, with the specified category and no target
		/// object.
		/// </summary>
		/// <remarks>
		/// The record's error ID is the exception's type name, as <see cref="extension(Exception?).GetTypeName()"/>
		/// returns it.
		/// </remarks>
		/// <param name="category">The category that best describes the error.</param>
		/// <returns>
		/// A new <see cref="ErrorRecord"/> for <paramref name="exception"/>, or <see langword="null"/> when
		/// <paramref name="exception"/> is <see langword="null"/>.
		/// </returns>
		[return: NotNullIfNotNull(nameof(exception))]
		internal ErrorRecord? ToRecord(ErrorCategory category)
		{
			return exception.ToRecord(category, targetObj: null);
		}

		/// <summary>
		/// Creates a PowerShell <see cref="ErrorRecord"/> for an exception, with the specified category and target
		/// object.
		/// </summary>
		/// <remarks>
		/// The record's error ID is the exception's type name, as <see cref="extension(Exception?).GetTypeName()"/>
		/// returns it.
		/// </remarks>
		/// <param name="category">The category that best describes the error.</param>
		/// <param name="targetObj">
		/// The object that the cmdlet was acting on when the error occurred, or <see langword="null"/> for none.
		/// </param>
		/// <returns>
		/// A new <see cref="ErrorRecord"/> for <paramref name="exception"/>, or <see langword="null"/> when
		/// <paramref name="exception"/> is <see langword="null"/>.
		/// </returns>
		[return: NotNullIfNotNull(nameof(exception))]
		internal ErrorRecord? ToRecord(ErrorCategory category, object? targetObj)
		{
			return exception is null
				? null
				: new ErrorRecord(
					exception: exception,
					errorId: exception.GetTypeName(),
					errorCategory: category,
					targetObject: targetObj);
		}
	}
}
