using System;
using System.Diagnostics;
using System.Reflection;

namespace MG.Sonarr.Next.Reflection
{
    /// <summary>
    /// A utility class for loading assemblies of the current <see cref="AppDomain"/>.
    /// </summary>
    public static class AssemblyLoader
    {
        /// <summary>
        /// Gets all loaded assemblies in the specified <see cref="AppDomain"/>.
        /// </summary>
        /// <param name="appDomain">The <see cref="AppDomain"/> to get loaded assemblies from.</param>
        /// <returns>An array of assemblies loaded in the specified application domain.</returns>
        public static Assembly[] GetAppDomainAssemblies(AppDomain appDomain)
        {
            Assembly[] assemblies = appDomain.GetAssemblies();
            SortAssemblies(assemblies);

            return assemblies;
        }

        [Conditional("DEBUG")]
        private static void SortAssemblies(Assembly[] assemblies)
        {
            Array.Sort(assemblies, (x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.FullName, y.FullName));
        }
    }
}
