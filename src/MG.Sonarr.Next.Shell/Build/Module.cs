using MG.Sonarr.Next.Extensions.PSO;
using System.Reflection;

namespace MG.Sonarr.Next.Shell.Build
{
    public static class Module
    {
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
    }
}