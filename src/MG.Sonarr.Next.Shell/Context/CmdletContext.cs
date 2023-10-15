using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Json.Converters;
using MG.Sonarr.Next.Json.Converters.Spans;
using MG.Sonarr.Next.Json.Modifiers;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Calendar;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.Indexers;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Models.Qualities;
using MG.Sonarr.Next.Models.Releases;
using MG.Sonarr.Next.Models.RootFolders;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Shell.Cmdlets;
using MG.Sonarr.Next.Shell.Cmdlets.Connection;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Pools;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization.Metadata;
using MG.Sonarr.Next.Services.Jobs;
using MG.Sonarr.Next.Models.Commands;
using System.Text.Json;
using MG.Sonarr.Next.Services.Testing;

namespace MG.Sonarr.Next.Shell.Context
{
#if DEBUG
    public static class CmdletContextExtensions
#else
    internal static class CmdletContextExtensions
#endif
    {
        internal static IServiceScope CreateScope(this SonarrCmdletBase cmdlet)
        {


            return SonarrContext.GetProvider().CreateScope();
        }
        internal static IServiceScope CreateScope(this ConnectSonarrInstanceCmdlet cmdlet)
        {
            return SonarrContext.GetProvider().CreateScope();
        }

#if DEBUG
        public static IMetadataResolver GetResolver()
        {
            return SonarrContext.GetProvider().GetRequiredService<IMetadataResolver>();
        }
        public static JsonSerializerOptions GetSerializerOptions()
        {
            return SonarrContext.GetProvider().GetRequiredService<ISonarrJsonOptions>().GetForSerializing();
        }
#endif

        internal static IServiceProvider GetServiceProvider(this Cmdlet cmdlet)
        {
            return SonarrContext.GetProvider();
        }

        public static void SetContext(this ConnectSonarrInstanceCmdlet cmdlet, IConnectionSettings settings, Func<IServiceCollection, ServiceProviderOptions, IServiceProvider> buildProvider)
        {
            SonarrContext.Initialize(settings, buildProvider);
        }
        internal static void UnsetContext(this DisconnectSonarrInstanceCmdlet cmdlet)
        {
            SonarrContext.Deinitialize();
        }
        internal static void UnsetContext(this ConnectSonarrInstanceCmdlet cmdlet)
        {
            SonarrContext.Deinitialize();
        }
    }

    file static class SonarrContext
    {
        static IServiceProvider _provider = null!;

        /// <exception cref="InvalidOperationException"/>
        internal static IServiceProvider GetProvider()
        {
            return _provider ?? throw new InvalidOperationException("The Sonarr context is not set.");
        }

        internal static void Deinitialize()
        {
            _provider = null!;
        }

        internal static void Initialize(IConnectionSettings settings, Func<IServiceCollection, ServiceProviderOptions, IServiceProvider> buildProvider)
        {
            if (_provider is not null)
            {
                return;
            }

            ServiceCollection services = new();
            services
                .AddMemoryCache()
                .AddSingleton<Queue<IApiCmdlet>>()
                .AddSonarrClient(settings, (provider, options) =>
                {
                    options.WriteIndented = true;
                })
                .AddCommandTracker()
                .AddTestingService();

            AddObjectPools(services);

            ServiceProviderOptions providerOptions = new()
            {
#if !RELEASE
                ValidateOnBuild = true,
                ValidateScopes = true,
#else
                ValidateOnBuild = false,
                ValidateScopes = false,
#endif
            };

            _provider = buildProvider(services, providerOptions);
        }

        private static void AddObjectPools(IServiceCollection services)
        {
            services.AddObjectPoolReturner();

            AddPool<HashSet<Wildcard>, HashSetWildcardPool>(services);
            AddPool<SortedSet<int>, SortedIntSetPool>(services);
            AddPool<PagingParameter, PagingPool>(services);
            AddPool<Stopwatch, StopwatchPool>(services);
        }
        private static void AddPool<T, TPool>(IServiceCollection services) 
            where TPool : class, IObjectPoolReturnable, IObjectPool<T>, new()
            where T : notnull
        {
            TPool pool = new TPool();
            services.AddSingleton<TPool>(pool)
                    .AddSingleton<IObjectPool<T>>(x => x.GetRequiredService<TPool>())
                    .AddSingleton<IObjectPoolReturnable>(x => x.GetRequiredService<TPool>());
        }
    }
}