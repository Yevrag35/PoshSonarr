using System.Management.Automation;

namespace MG.Sonarr.Next.Extensions
{
    /// <summary>
    /// Custom extension methods for <see cref="Exception"/> classes.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Returns the <see cref="Exception"/> class's type name.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <inheritdoc cref="TypeExtensions.GetTypeName(Type?)"/>
        [return: NotNullIfNotNull(nameof(exception))]
        public static string? GetTypeName(this Exception? exception)
        {
            Type? type = exception?.GetType();
            return TypeExtensions.GetTypeName(type);
        }

        /// <summary>
        /// Projects the <see cref="Exception"/> to a PowerShell <see cref="ErrorRecord"/>.
        /// </summary>
        /// <remarks>
        ///     Only if <paramref name="exception"/> is <see langword="null"/>, will the resulting
        ///     <see cref="ErrorRecord"/> will <see langword="null"/>.
        /// </remarks>
        /// <param name="exception">The exception which describes the error.</param>
        /// <returns>
        ///     A new <see cref="ErrorRecord"/> instance constructed from
        ///     <paramref name="exception"/> with the default <see cref="ErrorCategory"/>.
        /// </returns>
        [return: NotNullIfNotNull(nameof(exception))]
        public static ErrorRecord? ToRecord(this Exception? exception)
        {
            return ToRecord(exception, ErrorCategory.NotSpecified, null);
        }

        /// <summary>
        /// Projects the <see cref="Exception"/> to a PowerShell <see cref="ErrorRecord"/>.
        /// </summary>
        /// <param name="exception">The exception which describes the error.</param>
        /// <param name="category">
        ///     The <see cref="ErrorCategory"/> which best describes the error.
        /// </param>
        /// <returns>
        ///     A new <see cref="ErrorRecord"/> instance constructed from
        ///     <paramref name="exception"/> with the specified <see cref="ErrorCategory"/>.
        /// </returns>
        [return: NotNullIfNotNull(nameof(exception))]
        public static ErrorRecord? ToRecord(this Exception? exception, ErrorCategory category)
        {
            return ToRecord(exception, category, null);
        }

        /// <summary>
        /// Projects the <see cref="Exception"/> to a PowerShell <see cref="ErrorRecord"/>.
        /// </summary>
        /// <param name="exception">The exception which describes the error.</param>
        /// <param name="category">
        ///     The <see cref="ErrorCategory"/> which best describes the error.
        /// </param>
        /// <param name="targetObj">
        ///     This is the object against which the cmdlet or provider was operating when the
        ///     error occurred. This is optional.
        /// </param>
        /// <returns>
        ///     A new <see cref="ErrorRecord"/> instance constructed from
        ///     <paramref name="exception"/> with the specified <see cref="ErrorCategory"/>
        ///     and target object.
        /// </returns>
        [return: NotNullIfNotNull(nameof(exception))]
        public static ErrorRecord? ToRecord(this Exception? exception, ErrorCategory category, object? targetObj)
        {
            if (exception is null)
            {
                return null;
            }

            Type type = exception.GetType();
            return new ErrorRecord(exception, type.FullName ?? type.Name, category, targetObj);
        }
    }
}
