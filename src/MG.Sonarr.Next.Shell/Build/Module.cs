﻿using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Shell.Cmdlets.Connection;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Shell.Build
{
    public static class Module
    {
#if DEBUG
        public static Dictionary<string, SortedSet<string>> GetCmdletsToTags()
        {
            return MetadataResolverDI.FindPipeableCmdlets(typeof(ConnectSonarrInstanceCmdlet).Assembly);
        }
#endif

        public static PSObject ReadAllAssemblyCmdlets()
        {
            List<Type> list = GetCmdletTypes();

            List<string> names = new(list.Count);
            List<string> aliases = new(list.Count / 4);

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
            names.Add($"{cmdletAtt.VerbName}-{cmdletAtt.NounName}");

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
        private static bool IsCmdlet(Type type)
        {
            return type.IsDefined(typeof(CmdletAttribute), false);
        }
    }
}