using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using System.Reflection;

namespace MG.Sonarr.Next.Shell.Build
{
    public static class Module
    {
        public static PSObject GetFormatsAndTypePaths(string outputDir)
        {
            ArgumentException.ThrowIfNullOrEmpty(outputDir);

            ReadOnlySpan<char> formatStr = ".Format.ps1xml";
            ReadOnlySpan<char> typeStr = ".Type.ps1xml";
            FileTypes paths = new(formatStr, typeStr, outputDir);

            foreach (string file in Directory.EnumerateFiles(outputDir, "*.ps1xml", SearchOption.AllDirectories))
            {
                paths.AddPath(file);
            }

            PSObject pso = new(2);
            pso.Properties.Add(new PSNoteProperty("Formats", paths.FormatPaths.ToArray()));
            pso.Properties.Add(new PSNoteProperty("Types", paths.TypePaths.ToArray()));

            return pso;
        }
        public static PSObject ReadAllAssemblyCmdlets()
        {
            List<Type> list = GetCmdletTypes();

            List<string> names = new(list.Count);
            List<string> aliases = new(list.Count / 2);

            foreach (Type type in list)
            {
                AddCmdletData(type, names, aliases);
            }

            names.Sort();
            aliases.Sort();

            PSObject pso = new(2);
            pso.AddProperty("Cmdlets", names.ToArray());
            pso.AddProperty("Aliases", aliases.ToArray());

            return pso;
        }
        private static void AddCmdletData(Type type, List<string> names, List<string> aliases)
        {
            CmdletAttribute cmdletAtt = type.GetCustomAttributes<CmdletAttribute>().First();
            names.Add(GetCmdletName(cmdletAtt));

            if (type.IsDefined(typeof(AliasAttribute), false))
            {
                foreach (AliasAttribute alias in type.GetCustomAttributes<AliasAttribute>())
                {
                    if (alias.AliasNames is not null)
                    {
                        aliases.AddRange(alias.AliasNames);
                    }
                }
            }
        }
        private static List<Type> GetCmdletTypes()
        {
            Assembly thisAss = typeof(Module).Assembly;

            IEnumerable<Type> cmdletTypes = thisAss
                .GetExportedTypes()
                    .Where(IsCmdlet);

            return new(cmdletTypes);
        }
        const char DASH = '-';
        private static string GetCmdletName(CmdletAttribute cmdletAttribute)
        {
            int length = cmdletAttribute.VerbName.Length + cmdletAttribute.NounName.Length + 1;
            return string.Create(length, cmdletAttribute, (chars, state) =>
            {
                state.VerbName.CopyTo(chars);
                int position = state.VerbName.Length;

                chars[position++] = DASH;

                state.NounName.CopyTo(chars.Slice(position));
            });
        }
        private static bool IsCmdlet(Type type)
        {
            return type.IsDefined(typeof(CmdletAttribute), false);
        }

        private readonly ref struct FileTypes
        {
            readonly ReadOnlySpan<char> _formatStr;
            readonly ReadOnlySpan<char> _outputDir;
            readonly ReadOnlySpan<char> _typeStr;

            internal readonly List<string> FormatPaths;
            internal readonly List<string> TypePaths;

            internal FileTypes(ReadOnlySpan<char> formatStr, ReadOnlySpan<char> typeStr, ReadOnlySpan<char> outputDir)
            {
                _formatStr = formatStr;
                _outputDir = outputDir;
                _typeStr = typeStr;
                FormatPaths = new(10);
                TypePaths = new(1);
            }

            internal void AddPath(string filePath)
            {
                ReadOnlySpan<char> path = filePath.AsSpan();
                if (path.EndsWith(_formatStr, StringComparison.InvariantCultureIgnoreCase)
                    &&
                    TryGetFinalPath(_outputDir, path, out string? finalPath))
                {
                    FormatPaths.Add(finalPath);
                }
                else if (path.EndsWith(_typeStr, StringComparison.InvariantCultureIgnoreCase)
                    &&
                    TryGetFinalPath(_outputDir, path, out string? typePath))
                {
                    TypePaths.Add(typePath);
                }
            }
            private static bool TryGetFinalPath(ReadOnlySpan<char> outputDir, ReadOnlySpan<char> path, [NotNullWhen(true)] out string? finalPath)
            {
                finalPath = null;
                foreach (ReadOnlySpan<char> section in path.SpanSplit(outputDir))
                {
                    if (section.StartsWith('/') || section.StartsWith('\\'))
                    {
                        finalPath = new string(section.Slice(1));
                        return true;
                    }
                }

                return false;
            }
        }
    }
}