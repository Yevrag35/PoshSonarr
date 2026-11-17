using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Checkers;
using MG.Sonarr.Next.Shell.Components;
using System.Collections.Concurrent;

namespace MG.Sonarr.Next.Shell.Services;

/// <summary>
/// Provides the registration of the Shell project's own services in a connection's container.
/// </summary>
/// <remarks>
/// <c>Connect-SonarrInstance</c> passes <see cref="AddConfiguration(IServiceCollection)"/> to the connection that it
/// builds, and <c>Test-SonarrHost</c> to the provider of its own test, which add these services beside the HTTP client,
/// the JSON options, and the metadata tags.
/// </remarks>
internal static class ModuleServiceConfigurer
{
	/// <summary>
	/// Adds the Shell project's services to the specified collection.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="ManualImportEdit"/> is scoped, so each run of <c>Edit-SonarrManualImport</c> gets its own, and so is
	/// <see cref="ReleaseProfileObject"/>. The cache of the comparers that
	/// <see cref="MG.Sonarr.Next.Shell.Attributes.DistinctValuesAttribute"/> uses, one for each element type, is a
	/// singleton, kept for the length of the connection and shared by every runspace.
	/// </para>
	/// <para>
	/// TODO: What resolves the scoped <see cref="ReleaseProfileObject"/>? Nothing in the solution asks the container for
	/// one.
	/// </para>
	/// </remarks>
	/// <param name="services">The connection's service collection.</param>
	internal static void AddConfiguration(IServiceCollection services)
	{
		services.AddScoped<ManualImportEdit>()
				.AddScoped<ReleaseProfileObject>()
				.AddSingleton<ConcurrentDictionary<Type, EqualityChecker>>(provider => []);
	}
}