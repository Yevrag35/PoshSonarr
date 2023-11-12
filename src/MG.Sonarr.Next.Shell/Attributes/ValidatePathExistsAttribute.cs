namespace MG.Sonarr.Next.Shell.Attributes
{
    public sealed class ValidatePathExistsAttribute : ValidateArgumentsAttribute
    {
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            string strPath = GetArgumentAsString(arguments);

            var col = engineIntrinsics.SessionState.Path.GetResolvedProviderPathFromPSPath(strPath, out _);
            if (col.Count <= 0)
            {
                ThrowPathNotFound(strPath);
            }
        }

        private static string GetArgumentAsString(object arguments)
        {
            return arguments switch
            {
                string strVal => strVal,
                IFormattable formattable => formattable.ToString(null, Statics.DefaultProvider),
                IConvertible convertible => Convert.ToString(convertible) ?? string.Empty,
                _ => ThrowIncorrectType<string>(arguments),
            };
        }

        [DoesNotReturn]
        private static T ThrowIncorrectType<T>(object arguments)
        {
            throw new ValidationMetadataException($"'{arguments?.ToString()}' is not and cannot be converted to a string.");
        }

        [DoesNotReturn]
        private static void ThrowPathNotFound(string path)
        {
            throw new ValidationMetadataException($"The specified path does not exist -> {path}");
        }
    }
}

