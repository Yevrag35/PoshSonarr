namespace MG.Sonarr.Next.Shell.Attributes;

/// <summary>
/// Represents the kinds of path that a path validation attribute, such as <see cref="ValidateIsLocalPathAttribute"/>,
/// expects, as its error message names them.
/// </summary>
/// <remarks>
/// <para>
/// The flags combine, as in <c>Absolute | UNC</c> for an absolute UNC path.
/// </para>
/// <para>
/// TODO: What does <see cref="Possible"/> mean? No code reads it, nor <see cref="Relative"/>.
/// </para>
/// </remarks>
[Flags]
public enum PathType
{
	/// <summary>
	/// No kind of path.
	/// </summary>
	None = 0,
	/// <summary>
	/// A value that may be a path.
	/// </summary>
	Possible = 1,

	/// <summary>
	/// A path on a local drive, rather than a UNC path.
	/// </summary>
	Local = 2,
	/// <summary>
	/// A UNC path, which names a share on a server.
	/// </summary>
	UNC = 4,

	/// <summary>
	/// A fully qualified path.
	/// </summary>
	Absolute = 16,
	/// <summary>
	/// A path relative to the current location.
	/// </summary>
	Relative = 32,
}

