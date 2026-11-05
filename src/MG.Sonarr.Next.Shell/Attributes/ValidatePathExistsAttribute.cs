namespace MG.Sonarr.Next.Shell.Attributes;

/// <summary>
/// Provides the validation of a path argument as a path to an item that exists.
/// </summary>
/// <remarks>
/// The path is resolved through the PowerShell provider, against the session's current location, so it can be
/// relative, hold a PowerShell drive, or hold wildcards, and it passes when it matches at least one item. An argument
/// that isn't a string is converted to one first, in the invariant culture.
/// </remarks>
public sealed class ValidatePathExistsAttribute : ValidateArgumentsAttribute
{
	/// <summary>
	/// Resolves the path that the argument gives and fails validation when it matches no item.
	/// </summary>
	/// <param name="arguments">The argument, a string or a value that converts to one.</param>
	/// <param name="engineIntrinsics">The engine APIs of the session that binds the argument, whose path APIs resolve
	/// the path.</param>
	/// <exception cref="ValidationMetadataException">Thrown when the argument can't be converted to a string, or when the
	/// path matches no item.</exception>
	protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
	{
		string strPath = GetArgumentAsString(arguments);

		var col = engineIntrinsics.SessionState.Path.GetResolvedProviderPathFromPSPath(strPath, out _);
		if (col.Count <= 0)
		{
			ThrowPathNotFound(strPath);
		}
	}

	/// <summary>
	/// Gets the argument as a string.
	/// </summary>
	/// <param name="arguments">The argument.</param>
	/// <returns>The string itself, or the argument formatted in the invariant culture when it's an
	/// <see cref="IFormattable"/> or an <see cref="IConvertible"/>.</returns>
	/// <exception cref="ValidationMetadataException">Thrown when the argument is none of those.</exception>
	private static string GetArgumentAsString(object arguments)
	{
		return arguments switch
		{
			string strVal => strVal,
			IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
			IConvertible convertible => Convert.ToString(convertible, CultureInfo.InvariantCulture) ?? string.Empty,
			_ => ThrowIncorrectType<string>(arguments),
		};
	}

	/// <summary>
	/// Fails validation for an argument that can't be converted to a string.
	/// </summary>
	/// <typeparam name="T">The type that the caller's expression expects, which lets a <see langword="switch"/>
	/// expression arm call the method.</typeparam>
	/// <param name="arguments">The argument, which the message shows.</param>
	/// <returns>Never returns.</returns>
	/// <exception cref="ValidationMetadataException">Always thrown.</exception>
	[DoesNotReturn]
	private static T ThrowIncorrectType<T>(object arguments)
	{
		throw new ValidationMetadataException($"'{arguments?.ToString()}' is not and cannot be converted to a string.");
	}

	/// <summary>
	/// Fails validation for a path that matches no item.
	/// </summary>
	/// <param name="path">The path, which the message shows.</param>
	/// <exception cref="ValidationMetadataException">Always thrown.</exception>
	[DoesNotReturn]
	private static void ThrowPathNotFound(string path)
	{
		throw new ValidationMetadataException($"The specified path does not exist -> {path}");
	}
}

