using System.Management.Automation;

namespace MG.Sonarr.Next.Services.Extensions
{
    public static class ExceptionExtensions
    {
        public static ErrorRecord ToRecord(this Exception exception)
        {
            return ToRecord(exception, ErrorCategory.NotSpecified);
        }
        public static ErrorRecord ToRecord(this Exception exception, ErrorCategory category, object? targetObj = null)
        {
            Type type = exception.GetType();
            return new ErrorRecord(exception, type.FullName ?? type.Name, category, targetObj);
        }
    }
}
