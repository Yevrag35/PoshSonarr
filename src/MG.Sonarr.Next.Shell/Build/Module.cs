using System.Reflection;

namespace MG.Sonarr.Next.Shell.Build;

public static class Module
{
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
	private static Type[] GetCmdletTypes()
	{
		Assembly thisAss = typeof(Module).Assembly;

		return [.. thisAss.GetExportedTypes().Where(x => x.IsDefined(typeof(CmdletAttribute), inherit: false))];
	}

	private static string GetCmdletName(CmdletAttribute cmdletAttribute)
	{
		return string.Concat(cmdletAttribute.VerbName, ['-'], cmdletAttribute.NounName);
	}

	private readonly ref struct FileTypes
	{
		readonly ReadOnlySpan<char> _formatStr;
		readonly ReadOnlySpan<char> _typeStr;

		internal readonly HashSet<string> FormatPaths;
		internal readonly HashSet<string> TypePaths;

		internal FileTypes(ReadOnlySpan<char> formatStr, ReadOnlySpan<char> typeStr)
		{
			_formatStr = formatStr;
			_typeStr = typeStr;
			FormatPaths = new(10, StringComparer.OrdinalIgnoreCase);
			TypePaths = new(1, StringComparer.OrdinalIgnoreCase);
		}

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

			finalPath = Path.Join(parentDirName, fileName);
			return true;
		}
	}
}