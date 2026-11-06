using System.Reflection;

namespace MG.Sonarr.Next.Shell.Build;

/// <summary>
/// Provides the lists that the module's manifest exports: the cmdlets and their aliases, and the format and type files.
/// </summary>
/// <remarks>
/// The packaging script that a Release build runs imports the module's root assembly and calls these methods, then
/// passes their lists to <c>New-ModuleManifest</c> as <c>CmdletsToExport</c>, <c>AliasesToExport</c>,
/// <c>FormatsToProcess</c>, and <c>TypesToProcess</c>. Each list is sorted ordinally, so the manifest's order doesn't
/// change from one build to the next.
/// </remarks>
public static class Module
{
	/// <summary>
	/// Finds the format and type files in the specified output folder and its subfolders.
	/// </summary>
	/// <remarks>
	/// A format file's name ends with <c>.Format.ps1xml</c> and a type file's with <c>.Type.ps1xml</c>, ignoring case.
	/// Each path is meant to be relative to the output folder, where the manifest goes, and is given as the file's name
	/// after the name of the folder that holds it, such as <c>PSFormats\Series.Format.ps1xml</c>.
	/// </remarks>
	/// <param name="outputDir">The folder that the build writes the module to.</param>
	/// <returns>
	/// A dictionary, with keys that ignore case, whose <c>Formats</c> key holds the format files' paths and whose
	/// <c>Types</c> key holds the type files' paths, each sorted ordinally and empty when there are none.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="outputDir"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="outputDir"/> is empty.</exception>
	/// <exception cref="DirectoryNotFoundException">Thrown when <paramref name="outputDir"/> doesn't exist.</exception>
	public static Dictionary<string, string[]> GetFormatsAndTypePaths(string outputDir)
	{
		ArgumentException.ThrowIfNullOrEmpty(outputDir);

		ReadOnlySpan<char> formatStr = ".Format.ps1xml";
		ReadOnlySpan<char> typeStr = ".Type.ps1xml";
		FileTypes paths = new(formatStr, typeStr);

		foreach (string file in Directory.EnumerateFiles(outputDir, "*.ps1xml", SearchOption.AllDirectories))
		{
			paths.AddPath(file);
		}

		return new(2, StringComparer.OrdinalIgnoreCase)
		{
			{ "Formats", paths.FormatPaths.Count > 0 ? [.. paths.FormatPaths.Order(StringComparer.Ordinal)] : [] },
			{ "Types", paths.TypePaths.Count > 0 ? [.. paths.TypePaths.Order(StringComparer.Ordinal)] : [] },
		};
	}
	/// <summary>
	/// Reads the names and aliases of the cmdlets that the Shell assembly exports.
	/// </summary>
	/// <remarks>
	/// A cmdlet is a public type marked with <see cref="CmdletAttribute"/>, and its name is the attribute's verb and noun
	/// joined with a hyphen. Its aliases are the names of every <see cref="AliasAttribute"/> on the type. A name or alias
	/// that appears more than once, ignoring case, is listed once.
	/// </remarks>
	/// <returns>
	/// A dictionary, with keys that ignore case, whose <c>Cmdlets</c> key holds the cmdlets' names and whose
	/// <c>Aliases</c> key holds their aliases, each sorted ordinally, or an empty dictionary when the assembly exports no
	/// cmdlet.
	/// </returns>
	public static Dictionary<string, string[]> ReadAllAssemblyCmdlets()
	{
		Type[] list = GetCmdletTypes();
		if (list.Length == 0)
			return [];

		HashSet<string> names = new(list.Length, StringComparer.OrdinalIgnoreCase);
		HashSet<string> aliases = new((int)Math.Ceiling(list.Length / 2d), StringComparer.OrdinalIgnoreCase);

		foreach (Type type in list)
		{
			AddCmdletData(type, names, aliases);
		}

		return new(2, StringComparer.OrdinalIgnoreCase)
		{
			{ "Cmdlets", [.. names.Order(StringComparer.Ordinal)] },
			{ "Aliases", [.. aliases.Order(StringComparer.Ordinal)] },
		};
	}
	/// <summary>
	/// Adds a cmdlet type's name and aliases to the specified sets.
	/// </summary>
	/// <param name="type">The cmdlet type, which carries a <see cref="CmdletAttribute"/>.</param>
	/// <param name="names">The set of cmdlet names, which the method adds the cmdlet's name to.</param>
	/// <param name="aliases">The set of aliases, which the method adds the names of the type's
	/// <see cref="AliasAttribute"/> attributes to.</param>
	private static void AddCmdletData(Type type, HashSet<string> names, HashSet<string> aliases)
	{
		CmdletAttribute cmdletAtt = type.GetCustomAttributes<CmdletAttribute>().First();
		_ = names.Add(GetCmdletName(cmdletAtt));

		if (type.IsDefined(typeof(AliasAttribute), false))
		{
			foreach (AliasAttribute alias in type.GetCustomAttributes<AliasAttribute>())
			{
				if (alias.AliasNames is not null)
				{
					aliases.UnionWith(alias.AliasNames);
				}
			}
		}
	}
	/// <summary>
	/// Gets the public types of the Shell assembly that carry a <see cref="CmdletAttribute"/> of their own.
	/// </summary>
	/// <returns>The cmdlet types, or an empty array when there are none.</returns>
	private static Type[] GetCmdletTypes()
	{
		Assembly thisAss = typeof(Module).Assembly;

		return [.. thisAss.GetExportedTypes().Where(x => x.IsDefined(typeof(CmdletAttribute), inherit: false))];
	}

	/// <summary>
	/// Gets the name of a cmdlet from its attribute.
	/// </summary>
	/// <param name="cmdletAttribute">The cmdlet's attribute.</param>
	/// <returns>The verb and noun joined with a hyphen, such as <c>Get-SonarrSeries</c>.</returns>
	private static string GetCmdletName(CmdletAttribute cmdletAttribute)
	{
		return string.Concat(cmdletAttribute.VerbName, ['-'], cmdletAttribute.NounName);
	}

	/// <summary>
	/// Represents the sets of format and type file paths that <see cref="GetFormatsAndTypePaths(string)"/> collects.
	/// </summary>
	/// <remarks>
	/// The struct holds the two file name suffixes as spans, so it's a <see langword="ref"/> struct that lives on the
	/// stack of the method that fills it. The sets ignore case.
	/// </remarks>
	private readonly ref struct FileTypes
	{
		readonly ReadOnlySpan<char> _formatStr;
		readonly ReadOnlySpan<char> _typeStr;

		/// <summary>
		/// The paths of the format files.
		/// </summary>
		internal readonly HashSet<string> FormatPaths;
		/// <summary>
		/// The paths of the type files.
		/// </summary>
		internal readonly HashSet<string> TypePaths;

		/// <summary>
		/// Initializes a new <see cref="FileTypes"/> instance with the specified suffixes of format and type file names.
		/// </summary>
		/// <param name="formatStr">The suffix of a format file's name, such as <c>.Format.ps1xml</c>.</param>
		/// <param name="typeStr">The suffix of a type file's name, such as <c>.Type.ps1xml</c>.</param>
		internal FileTypes(ReadOnlySpan<char> formatStr, ReadOnlySpan<char> typeStr)
		{
			_formatStr = formatStr;
			_typeStr = typeStr;
			FormatPaths = new(10, StringComparer.OrdinalIgnoreCase);
			TypePaths = new(1, StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Adds a file's path to the format or type set when its name ends with that set's suffix, ignoring case.
		/// </summary>
		/// <remarks>
		/// A file whose name has neither suffix isn't added, nor one that <see cref="TryGetFinalPath"/> gives no path
		/// for.
		/// </remarks>
		/// <param name="path">The file's full path.</param>
		internal void AddPath(ReadOnlySpan<char> path)
		{
			if (path.EndsWith(_formatStr, StringComparison.OrdinalIgnoreCase)
				&&
				TryGetFinalPath(path, out string? finalPath))
			{
				FormatPaths.Add(finalPath);
			}
			else if (path.EndsWith(_typeStr, StringComparison.OrdinalIgnoreCase)
				&&
				TryGetFinalPath(path, out string? typePath))
			{
				TypePaths.Add(typePath);
			}
		}
		/// <summary>
		/// Gets the path that the manifest lists for a file, relative to the module's folder.
		/// </summary>
		/// <remarks>
		/// The path is the name of the folder that holds the file, followed by the file's name, as for a file in a
		/// folder such as <c>PSFormats</c> directly under the output folder. No path is given when the folder and the
		/// file have the same name.
		/// </remarks>
		/// <param name="path">The file's full path.</param>
		/// <param name="finalPath">When the method returns <see langword="true"/>, the file's relative path; otherwise,
		/// <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if the method gives a path; otherwise, <see langword="false"/>.</returns>
		private static bool TryGetFinalPath(ReadOnlySpan<char> path, [NotNullWhen(true)] out string? finalPath)
		{
			ReadOnlySpan<char> parentPath = Path.GetDirectoryName(path);
			ReadOnlySpan<char> parentDirName = Path.GetFileName(parentPath);
			ReadOnlySpan<char> fileName = Path.GetFileName(path);

			if (parentDirName.Equals(fileName, StringComparison.Ordinal))
			{
				finalPath = null;
				return false;
			}

			finalPath = Path.Join(parentPath, fileName);
			return true;
		}
	}
}