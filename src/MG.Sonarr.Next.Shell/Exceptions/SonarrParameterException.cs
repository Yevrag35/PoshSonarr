using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Extensions;

namespace MG.Sonarr.Next.Shell.Exceptions
{
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
        //static readonly ParameterErrorType[] _allErrorsTypes = Enum.GetValues<ParameterErrorType>();

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
            string typeStr = type.ToString();
            ReadOnlySpan<char> typeSpan = typeStr;
            scoped ReadOnlySpan<char> splitBy = stackalloc char[] { ',', ' ' };
            int count = GetErrorCount(typeSpan, splitBy);

            Span<char> destination = stackalloc char[typeSpan.Length + 4];
            if (count < 2)
            {
                typeSpan.Slice(0, 1).ToLower(destination, Statics.DefaultCulture);
                typeSpan.Slice(1).CopyTo(destination.Slice(1));
                return new string(destination.Slice(0, typeSpan.Length));
            }

            int written = 0;
            int i = 0;
            foreach (ReadOnlySpan<char> section in typeSpan.SpanSplit(splitBy))
            {
                section.Slice(0, 1).ToLower(destination.Slice(written), Statics.DefaultCulture);
                written++;

                section.Slice(1).CopyToSlice(destination, ref written);

                if (i < count - 1)
                {
                    if (count > 2)
                    {
                        splitBy.CopyToSlice(destination, ref written);
                    }
                    else
                    {
                        splitBy.Slice(1).CopyToSlice(destination, ref written);
                    }
                }

                i++;

                if (i == count - 1)
                {
                    AND.CopyToSlice(destination, ref written);
                }
            }

            return new string(destination.Slice(0, written));
        }

        private static int GetErrorCount(ReadOnlySpan<char> typeSpan, ReadOnlySpan<char> splitBy)
        {
            int count = 0;
            foreach (ReadOnlySpan<char> _ in typeSpan.SpanSplit(splitBy))
            {
                count++;
            }

            return count;
        }

        public SonarrErrorRecord ToRecord()
        {
            return this.ToRecord(this.ParameterName);
        }
        public SonarrErrorRecord ToRecord(object? targetObj)
        {
            return new SonarrErrorRecord(this, nameof(SonarrParameterException), ErrorCategory.InvalidArgument, targetObj);

        }

        //private static ReadOnlySpan<char> GetFormat(ReadOnlySpan<char> additionalMsg, out int )
        //{

        //}
    }
}
