using MG.Sonarr.Next.Extensions;

namespace MG.Sonarr.Next.Exceptions
{
    public class InvalidTypeException : PoshSonarrException
    {
        public Type? ActualType { get; }
        public Type ExpectedType { get; }

        protected InvalidTypeException(string msgFormat, Type expectedType, object? actualValue, Exception? innerException)
            : base(GetMessage(msgFormat, ref expectedType, actualValue, out Type? actualType), innerException)
        {
            this.ActualType = actualType;
            this.ExpectedType = expectedType;
        }
        private static string GetMessage(string msgFormat, ref Type keyType, object? key, out Type? actualType)
        {
            keyType ??= typeof(object);
            actualType = key?.GetType();

            string actual = actualType?.GetTypeName() ?? "null";

            return string.Format(msgFormat, keyType.GetTypeName(), actual);
        }
    }

    public sealed class InvalidKeyTypeException : InvalidTypeException
    {
        const string DEF_MSG = "The dictionary key was not of the correct type - expected '{0}' and got '{1}'.";

        public InvalidKeyTypeException(Type keyType, object? key)
            : this(keyType, key, null)
        {
        }
        public InvalidKeyTypeException(Type keyType, object? key, Exception? innerException)
            : base(DEF_MSG, keyType, key, innerException)
        {
        }
    }
    public sealed class InvalidValueTypeException : InvalidTypeException
    {
        const string DEF_MSG = "The dictionary value was not of the correct type - expected '{0}' and got '{1}'.";

        public InvalidValueTypeException(Type valueType, object? value)
            : this(valueType, value, null)
        {
        }
        public InvalidValueTypeException(Type valueType, object? value, Exception? innerException)
            : base(DEF_MSG, valueType, value, innerException)
        {
        }
    }
}

