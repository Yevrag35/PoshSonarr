using MG.Sonarr.Next.Collections.Pools;
using MG.Sonarr.Next.Metadata;
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
                .AddSingleton<ConcurrentDictionary<Type, EqualityChecker>>(provider => new(Environment.ProcessorCount, 4))
                .AddGenericObjectPool<Dictionary<int, IEpisodeBySeriesPipeable>>(builder =>
                {
                    builder.SetConstructor(() => new Dictionary<int, IEpisodeBySeriesPipeable>(50))
                            .SetDeconstructor(dict =>
                            {
                                dict.Clear();
                                int cap = dict.EnsureCapacity(50);
                                if (cap >= 1000)
                                {
                                    dict.TrimExcess(50);
                                }

                                return true;
                            });
                })
                .AddGenericObjectPool<HashSet<DayOfWeek>>(set =>
                {
                    int count = set.Count;
                    set.Clear();
                    return count <= 1000;
                })
                .AddGenericObjectPool<SortedSet<SonarrProperty>>(set =>
                {
                    int count = set.Count;
                    set.Clear();

                    return count <= 3000;
                });
    }
}