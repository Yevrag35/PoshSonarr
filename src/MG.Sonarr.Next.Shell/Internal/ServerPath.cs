namespace MG.Sonarr.Next.Shell.Internal;

/// <summary>
/// Provides checks of a path on the computer where Sonarr runs, which can take the form of a Linux, macOS, or Windows
/// path, whatever platform the session runs on.
/// </summary>
/// <remarks>
/// The checks read only a path's first characters, the same way on every platform, and never ask a PowerShell
/// provider or the file system, since neither knows anything about the computer where Sonarr runs. So a session on
/// Windows can name a path such as <c>/downloads/tv</c> for a Sonarr on Linux, and a session on Linux or macOS can
/// name one such as <c>C:\tv</c> for a Sonarr on Windows.
/// </remarks>
internal static class ServerPath
{
	/// <summary>
	/// Determines whether the specified path is fully qualified on Linux, macOS, or Windows, so it names the same
	/// location from any current directory.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A path that starts with two separators, each a <c>\</c> or <c>/</c>, as in <c>\\server\tv</c> or
	/// <c>//server/tv</c>, is a UNC path, and it counts only when <paramref name="allowUnc"/> is <see langword="true"/>.
	/// Otherwise, a path that starts with <c>/</c>, or with an ASCII letter, a colon, and a separator, as in
	/// <c>C:\tv</c>, is fully qualified.
	/// </para>
	/// <para>
	/// Any other path isn't, such as a relative path, a drive-relative one such as <c>C:tv</c>, a root-relative one
	/// such as <c>\tv</c>, one that starts with <c>~</c>, and a PowerShell path, such as one on a drive whose name is
	/// longer than a letter or one that names its provider.
	/// </para>
	/// </remarks>
	/// <param name="path">The path to check.</param>
	/// <param name="allowUnc">
	/// <see langword="true"/> to count a UNC path as fully qualified; otherwise, <see langword="false"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="path"/> is fully qualified; otherwise, <see langword="false"/>.
	/// </returns>
	internal static bool IsFullyQualified(ReadOnlySpan<char> path, bool allowUnc)
	{
		if (path.Length >= 2 && IsSeparator(path[0]) && IsSeparator(path[1]))
		{
			return allowUnc;
		}

		if (path.Length > 0 && path[0] == '/')
		{
			return true;
		}

		return path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && IsSeparator(path[2]);
	}

	/// <summary>
	/// Removes the trailing separators from the specified path, but not the separators of its root.
	/// </summary>
	/// <remarks>
	/// The root is the <c>\\</c> or <c>//</c> that starts a UNC path, the <c>/</c> or <c>\</c> that starts any other
	/// rooted path, or a drive letter, a colon, and a separator, as in <c>C:\</c>. So <c>/downloads/</c> becomes
	/// <c>/downloads</c> and <c>\\server\share\</c> becomes <c>\\server\share</c>, while <c>/</c> and <c>C:\</c> stay as
	/// they are. A path without a root, such as a relative one, loses every trailing separator.
	/// </remarks>
	/// <param name="path">The path to trim.</param>
	/// <returns>
	/// The path without its trailing separators, or <paramref name="path"/> itself when it ends in none past its root.
	/// </returns>
	internal static string TrimEndSeparators(string path)
	{
		int rootLength = GetRootLength(path);
		int end = path.Length;
		while (end > rootLength && IsSeparator(path[end - 1]))
		{
			end--;
		}

		return end == path.Length ? path : path[..end];
	}

	/// <summary>
	/// Gets the length of the specified path's root, in the forms of Linux, macOS, and Windows.
	/// </summary>
	/// <param name="path">The path whose root is measured.</param>
	/// <returns>
	/// 2 for the two separators that start a UNC path, 1 for the separator that starts any other rooted path, 3 for a
	/// drive letter, a colon, and a separator, and 0 for a path without a root.
	/// </returns>
	private static int GetRootLength(ReadOnlySpan<char> path)
	{
		if (path.Length >= 2 && IsSeparator(path[0]) && IsSeparator(path[1]))
		{
			return 2;
		}

		if (path.Length >= 1 && IsSeparator(path[0]))
		{
			return 1;
		}

		return path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && IsSeparator(path[2]) ? 3 : 0;
	}

	/// <summary>
	/// Determines whether the specified character separates a path's parts on Linux, macOS, or Windows.
	/// </summary>
	/// <param name="c">The character to check.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="c"/> is <c>\</c> or <c>/</c>; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool IsSeparator(char c)
	{
		return c is '\\' or '/';
	}
}
