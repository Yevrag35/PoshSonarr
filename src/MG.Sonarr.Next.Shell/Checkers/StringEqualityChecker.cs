namespace MG.Sonarr.Next.Shell.Checkers;

/// <summary>
/// Represents an equality checker for strings that compares them ordinally, ignoring case.
/// </summary>
/// <remarks>
/// A value that isn't a string is converted to one as PowerShell converts it before the comparison. The checker holds no
/// state, so it's thread-safe.
/// </remarks>
[DebuggerStepThrough]
public sealed class StringEqualityChecker : EqualityChecker<string>
{
	/// <summary>
	/// Determines whether two strings are equal, compared ordinally and ignoring case.
	/// </summary>
	/// <param name="x">The first string, which isn't <see langword="null"/>.</param>
	/// <param name="y">The second string, which isn't <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="x"/> and <paramref name="y"/> are equal, ignoring case; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	protected override bool Equals([DisallowNull] string x, [DisallowNull] string y)
	{
		return StringComparer.OrdinalIgnoreCase.Equals(x, y);
	}
	/// <summary>
	/// Gets a hash code for the specified object that ignores the case of a string.
	/// </summary>
	/// <param name="obj">The object, which isn't <see langword="null"/>.</param>
	/// <returns>
	/// The ordinal hash code of <paramref name="obj"/> ignoring case when it's a string; otherwise, its own hash code.
	/// </returns>
	protected override int GetHashCodeCore([DisallowNull] object obj)
	{
		return StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
	}
}