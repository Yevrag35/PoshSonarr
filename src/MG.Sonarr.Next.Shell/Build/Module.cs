using MG.Sonarr.Next.Extensions.PSO;
using System.Reflection;

namespace MG.Sonarr.Next.Shell.Build
{
    public static class Module
    {
        public static PSObject ReadAllAssemblyCmdlets()
        {
            Assembly thisAss = typeof(Module).Assembly;
            List<Type> list = new(thisAss.GetExportedTypes());

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!IsCmdlet(list[i]))
                {
                    list.RemoveAt(i);
                }
            }

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

        private static bool IsCmdlet(Type type)
        {
            return type.IsDefined(typeof(CmdletAttribute), false);
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
    }
}