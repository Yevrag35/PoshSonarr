using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Checkers;
using MG.Sonarr.Next.Shell.Components;
using System.Collections.Concurrent;

namespace MG.Sonarr.Next.Shell.Services;

internal static class ModuleServiceConfigurer
{
	internal static void AddConfiguration(IServiceCollection services)
	{
		services.AddScoped<ManualImportEdit>()
				.AddScoped<ReleaseProfileObject>()
				.AddSingleton<ConcurrentDictionary<Type, EqualityChecker>>(provider => []);
	}
}