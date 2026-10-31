namespace MG.Sonarr.Next.Reflection;

/// <summary>
/// Provides the assemblies that an <see cref="AppDomain"/> has already loaded.
/// </summary>
/// <remarks>
/// The class loads no assembly itself.
/// </remarks>
public static class AssemblyLoader
{
	/// <summary>
	/// Gets all loaded assemblies in the specified <see cref="AppDomain"/>.
	/// </summary>
	/// <remarks>
	/// Debug builds sort the assemblies by their full names, so that a debugging session sees them in the same order each
	/// time. Release builds return them in the order that <see cref="AppDomain.GetAssemblies"/> gives.
	/// </remarks>
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
