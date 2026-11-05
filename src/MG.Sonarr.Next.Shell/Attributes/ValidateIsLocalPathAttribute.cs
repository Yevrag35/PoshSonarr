using MG.Sonarr.Next.Extensions.Reflection;
using System.Collections;

namespace MG.Sonarr.Next.Shell.Attributes;

/// <summary>
/// Provides a base class for validation attributes that check a path argument's kind, such as a local or a UNC path.
/// </summary>
/// <remarks>
/// <para>
/// The argument must be a string that isn't empty, or a list of them, whose paths are each checked on their own.
/// When the derived class allows a relative path, a path is resolved against the session's current location first,
/// through the PowerShell provider, and the derived class checks the path that results.
/// </para>
/// <para>
/// With <see cref="MustExist"/>, the path must also exist. A relative path that must exist is resolved to the first
/// item that it matches.
/// </para>
/// </remarks>
public abstract class ValidatePathTypeAttribute : ValidateArgumentsAttribute
{
	/// <summary>
	/// When overridden in a derived class, gets a value that indicates whether the attribute resolves a relative path
	/// against the session's current location.
	/// </summary>
	/// <value><see langword="true"/> to resolve a relative path before checking it; otherwise,
	/// <see langword="false"/>.</value>
	protected abstract bool AllowsRelativePaths { get; }
	/// <summary>
	/// Gets or sets a value that indicates whether the path must exist.
	/// </summary>
	/// <value><see langword="true"/> if a path that doesn't exist fails validation; otherwise, <see langword="false"/>.
	/// The default is <see langword="false"/>.</value>
	public bool MustExist { get; init; }

	/// <summary>
	/// Checks the path that the argument gives, or each path of a list argument.
	/// </summary>
	/// <remarks>
	/// A path is resolved when <see cref="AllowsRelativePaths"/> is <see langword="true"/>, checked for existence when
	/// <see cref="MustExist"/> is <see langword="true"/>, and then passed to
	/// <see cref="ValidatePath(string, PathIntrinsics)"/>.
	/// </remarks>
	/// <param name="arguments">The argument: a path, or a list of paths.</param>
	/// <param name="engineIntrinsics">The engine APIs of the session that binds the argument, whose path APIs resolve a
	/// relative path.</param>
	/// <exception cref="ValidationMetadataException">Thrown when a path isn't a string, is null or empty, can't be
	/// resolved, doesn't exist when <see cref="MustExist"/> requires it, or isn't of the kind that the derived class
	/// expects.</exception>
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

	/// <summary>
	/// When implemented in a derived class, checks that a path is of the kind that the attribute expects.
	/// </summary>
	/// <param name="resolvedPath">The path, resolved against the session's current location when
	/// <see cref="AllowsRelativePaths"/> is <see langword="true"/>.</param>
	/// <param name="pathIntrinsics">The session's path APIs.</param>
	/// <exception cref="ValidationMetadataException">Thrown when <paramref name="resolvedPath"/> isn't of the expected
	/// kind.</exception>
	protected abstract void ValidatePath(string resolvedPath, PathIntrinsics pathIntrinsics);

	/// <summary>
	/// Resolves a path against the session's current location without requiring it to exist.
	/// </summary>
	/// <param name="relativePath">The path, which can be relative or hold a PowerShell drive.</param>
	/// <param name="pathIntrinsics">The session's path APIs.</param>
	/// <returns>The provider path, such as a file system path, that <paramref name="relativePath"/> stands for.</returns>
	/// <exception cref="ValidationMetadataException">Thrown when the path can't be resolved.</exception>
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
	/// <summary>
	/// Resolves a path against the session's current location to an item that exists.
	/// </summary>
	/// <param name="relativePath">The path, which can be relative, hold a PowerShell drive, or hold wildcards.</param>
	/// <param name="pathIntrinsics">The session's path APIs.</param>
	/// <returns>The provider path of the first item that <paramref name="relativePath"/> matches.</returns>
	/// <exception cref="ValidationMetadataException">Thrown when no item matches the path or it can't be
	/// resolved.</exception>
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

	/// <summary>
	/// Fails validation for a path that isn't of the expected kind.
	/// </summary>
	/// <param name="path">The path that failed.</param>
	/// <param name="pathType">The kinds of path expected, which the message names.</param>
	/// <exception cref="ValidationMetadataException">Always thrown.</exception>
	[DoesNotReturn]
	protected static void ThrowIncorrectKind(string path, PathType pathType)
	{
		throw new ValidationMetadataException($"The parameter argument is NOT the correct path type. Expected a path that was: {pathType.ToString().Replace("None", string.Empty)}.");
	}
	/// <summary>
	/// Fails validation for an argument that isn't a string.
	/// </summary>
	/// <param name="arguments">The argument, whose type the message names, or <see langword="null"/>.</param>
	/// <param name="innerException">The exception that the failed conversion threw, if any.</param>
	/// <exception cref="ValidationMetadataException">Always thrown.</exception>
	[DoesNotReturn]
	protected static void ThrowIncorrectType(object? arguments, Exception? innerException)
	{
		string typeName = arguments?.GetType().GetName() ?? "null";
		throw new ValidationMetadataException($"The parameter argument is NOT of type 'string'. Instead, we got '{typeName}'.", innerException);
	}
	/// <summary>
	/// Gets the path that an argument holds, and fails validation when it isn't a string or is empty.
	/// </summary>
	/// <param name="element">The argument, or one element of a list argument.</param>
	/// <param name="path">When the method returns, the path.</param>
	/// <exception cref="ValidationMetadataException">Thrown when <paramref name="element"/> isn't a string, or is null
	/// or empty.</exception>
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

/// <summary>
/// Provides the validation of a path argument as an absolute local path, one that isn't a UNC path.
/// </summary>
/// <remarks>
/// With <see cref="AllowRelative"/>, a relative path, or one on a PowerShell drive, is resolved against the session's
/// current location, and the path that results is checked.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ValidateIsLocalPathAttribute : ValidatePathTypeAttribute
{
	/// <summary>
	/// Gets a value that indicates whether the attribute resolves a relative path, from <see cref="AllowRelative"/>.
	/// </summary>
	/// <value>The value of <see cref="AllowRelative"/>.</value>
	protected override bool AllowsRelativePaths => this.AllowRelative;
	/// <summary>
	/// Gets or sets a value that indicates whether a relative path is resolved against the session's current location
	/// instead of failing validation.
	/// </summary>
	/// <value><see langword="true"/> to resolve a relative path; otherwise, <see langword="false"/>. The default is
	/// <see langword="false"/>.</value>
	public bool AllowRelative { get; init; }

	/// <summary>
	/// Checks that a path is absolute and isn't a UNC path.
	/// </summary>
	/// <param name="resolvedPath">The path to check.</param>
	/// <param name="pathIntrinsics">The session's path APIs, which the method doesn't read.</param>
	/// <exception cref="ValidationMetadataException">Thrown when <paramref name="resolvedPath"/> isn't an absolute
	/// path, or is a UNC path.</exception>
	protected override void ValidatePath(string resolvedPath, PathIntrinsics pathIntrinsics)
	{
		if (!Uri.TryCreate(resolvedPath, UriKind.Absolute, out Uri? uri) || uri.IsUnc)
		{
			ThrowIncorrectKind(resolvedPath, PathType.Local);
		}
	}
}

/// <summary>
/// Provides the validation of a path argument as an absolute UNC path, one that names a share on a server.
/// </summary>
/// <remarks>
/// A relative path isn't resolved, so it fails validation.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ValidateIsUncPathAttribute : ValidatePathTypeAttribute
{
	/// <summary>
	/// Gets a value that indicates whether the attribute resolves a relative path, which it never does.
	/// </summary>
	/// <value>Always <see langword="false"/>.</value>
	protected override bool AllowsRelativePaths => false;

	/// <summary>
	/// Checks that a path is an absolute UNC path.
	/// </summary>
	/// <param name="resolvedPath">The path to check.</param>
	/// <param name="pathIntrinsics">The session's path APIs, which the method doesn't read.</param>
	/// <exception cref="ValidationMetadataException">Thrown when <paramref name="resolvedPath"/> isn't an absolute UNC
	/// path.</exception>
	protected override void ValidatePath(string resolvedPath, PathIntrinsics pathIntrinsics)
	{
		if (!Uri.TryCreate(resolvedPath, UriKind.Absolute, out Uri? uri) || !uri.IsUnc)
		{
			ThrowIncorrectKind(resolvedPath, PathType.Absolute | PathType.UNC);
		}
	}
}
