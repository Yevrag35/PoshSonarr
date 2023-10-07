using System.Management.Automation;

namespace MG.Sonarr.Next.Services.Extensions
{
    public static class ExceptionExtensions
    {
        [return: NotNullIfNotNull(nameof(exception))]
        public static string? GetTypeName(this Exception? exception)
        {
            Type? type = exception?.GetType();
            return TypeExtensions.GetTypeName(type);
        }

        [return: NotNullIfNotNull(nameof(exception))]
        public static ErrorRecord? ToRecord(this Exception? exception)
        {
            return ToRecord(exception, ErrorCategory.NotSpecified);
        }

        [return: NotNullIfNotNull(nameof(exception))]
        public static ErrorRecord? ToRecord(this Exception? exception, ErrorCategory category, object? targetObj = null)
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
