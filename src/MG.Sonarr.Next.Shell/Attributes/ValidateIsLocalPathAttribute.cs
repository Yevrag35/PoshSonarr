using MG.Sonarr.Next.Extensions;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

namespace MG.Sonarr.Next.Shell.Attributes
{
    public abstract class ValidatePathTypeAttribute : ValidateArgumentsAttribute
    {
        protected abstract bool AllowsRelativePaths { get; }
        public bool MustExist { get; init; }

        protected sealed override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (arguments is not string && arguments is IEnumerable enumerable)
            {
                foreach (object? arg in enumerable)
                {
                    this.Validate(arg, engineIntrinsics);
                }
            }
            else
            {
                ValidateStringIsNotEmpty(arguments, out string path);
                PathIntrinsics pathIntrinsics = engineIntrinsics.SessionState.Path;

                if (this.AllowsRelativePaths)
                {
                    path = this.MustExist
                        ? GetResolvedLocalPathFromRelative(path, pathIntrinsics)
                        : GetUnresolvedLocalPathFromRelative(path, pathIntrinsics);
                }
                else if (this.MustExist && !Path.Exists(path))
                {
                    throw new ValidationMetadataException($"The specified path does not exist -> {path}");
                }

                this.ValidatePath(path, pathIntrinsics);
            }
        }

        protected abstract void ValidatePath(string resolvedPath, PathIntrinsics pathIntrinsics);

        /// <exception cref="ValidationMetadataException"/>
        protected static string GetUnresolvedLocalPathFromRelative(string relativePath, PathIntrinsics pathIntrinsics)
        {
            try
            {
                return pathIntrinsics.GetUnresolvedProviderPathFromPSPath(relativePath);
            }
            catch (Exception e)
            {
                throw new ValidationMetadataException($"Unable to validate local path: \"{relativePath}\".", e);
            }
        }
        protected static string GetResolvedLocalPathFromRelative(string relativePath, PathIntrinsics pathIntrinsics)
        {
            try
            {
                var col = pathIntrinsics.GetResolvedProviderPathFromPSPath(relativePath, out _);
                return col.Count > 0
                    ? col[0]
                    : throw new ValidationMetadataException($"The specified path does not exist -> {relativePath}");
            }
            catch (Exception e)
            {
                throw new ValidationMetadataException($"The specified path does not exist -> {relativePath}", e);
            }
        }

        [DoesNotReturn]
        protected static void ThrowIncorrectKind(string path, PathType pathType)
        {
            throw new ValidationMetadataException($"The parameter argument is NOT the correct path type. Expected a path that was: {pathType.ToString().Replace("None", string.Empty)}.");
        }
        [DoesNotReturn]
        protected static void ThrowIncorrectType(object? arguments, Exception? innerException)
        {
            string typeName = arguments?.GetType().GetTypeName() ?? "null";
            throw new ValidationMetadataException($"The parameter argument is NOT of type 'string'. Instead, we got '{typeName}'.", innerException);
        }
        private static void ValidateStringIsNotEmpty(object? element, out string path)
        {
            path = string.Empty;

            try
            {
                path = (string?)element!;
                ArgumentException.ThrowIfNullOrEmpty(path);
            }
            catch (InvalidCastException castEx)
            {
                ThrowIncorrectType(element, castEx);
                return;
            }
            catch (ArgumentException argEx)
            {
                throw new ValidationMetadataException("The parameter cannot be an empty or entirely whitespace string.", argEx);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ValidateIsLocalPathAttribute : ValidatePathTypeAttribute
    {
        protected override bool AllowsRelativePaths => this.AllowRelative;
        public bool AllowRelative { get; init; }

        protected override void ValidatePath(string resolvedPath, PathIntrinsics pathIntrinsics)
        {
            if (!Uri.TryCreate(resolvedPath, UriKind.Absolute, out Uri? uri) || uri.IsUnc)
            {
                ThrowIncorrectKind(resolvedPath, PathType.Local);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ValidateIsUncPathAttribute : ValidatePathTypeAttribute
    {
        protected override bool AllowsRelativePaths => false;

        protected override void ValidatePath(string resolvedPath, PathIntrinsics pathIntrinsics)
        {
            if (!Uri.TryCreate(resolvedPath, UriKind.Absolute, out Uri? uri) || !uri.IsUnc)
            {
                ThrowIncorrectKind(resolvedPath, PathType.Absolute | PathType.UNC);
            }
        }
    }
}
