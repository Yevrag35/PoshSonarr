using MG.Http.Urls.Queries;
using MG.Sonarr.Next.Collections.Pools;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Services.Jobs;
using MG.Sonarr.Next.Services.Testing;
using MG.Sonarr.Next.Services.Time;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Exceptions;
using MG.Sonarr.Next.Shell.Pools;
using System.Reflection;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Context
{
#if DEBUG
    public static class CmdletContextExtensions
#else
    internal static class CmdletContextExtensions
#endif
    {
        internal static IServiceScope CreateScope<T>(this T cmdlet) where T : PSCmdlet, IIsRunning<T>, IScopeCmdlet<T>
        {
            //if (!T.IsRunningCommand(cmdlet))
            //{
            //    throw new InvalidOperationException("Don't execute me weird.");
            //}

            return SonarrContext.GetProvider().CreateScope();
        }

#if DEBUG
        [Obsolete("Only used in interactive PowerShell testing. Never should be called in the code directly.", error: true)]
        public static IMetadataResolver GetResolver()
        {
            return SonarrContext.GetProvider().GetRequiredService<IMetadataResolver>();
        }

        [Obsolete("Only used in interactive PowerShell testing. Never should be called in the code directly.", error: true)]
        public static JsonSerializerOptions GetSerializerOptions()
        {
            return SonarrContext.GetProvider().GetRequiredService<ISonarrJsonOptions>().ForSerializing;
        }
#endif

        internal static IServiceScope SetContext<T>(this T cmdlet, Assembly cmdletAssembly, Action<IServiceCollection> addAdditionalServices)
            where T : PSCmdlet, IConnectContextCmdlet, IIsRunning<T>
        {
            //if (!T.IsRunningCommand(cmdlet))
            //{
            //    throw new InvalidOperationException("Don't execute me weird.");
            //}

            return SonarrContext.Initialize(cmdlet.GetConnectionSettings(), cmdletAssembly, addAdditionalServices);
        }
        internal static void UnsetContext<T>(this T _) where T : IDisconnectContextCmdlet, IScopeCmdlet<T>
        {
            SonarrContext.Deinitialize();
        }
    }

    file static class SonarrContext
    {
        static IServiceProvider _provider = null!;

        /// <exception cref="ContextNotSetException"/>
        internal static IServiceProvider GetProvider()
        {
            return _provider ?? ThrowNotSet();
        }

        /// <exception cref="ContextNotSetException"></exception>
        [DoesNotReturn]
        private static IServiceProvider ThrowNotSet()
        {
            throw new ContextNotSetException(new CmdletScopeNotReadyException());
        }

        internal static void Deinitialize()
        {
            _provider = null!;
        }

        internal static IServiceScope Initialize(IConnectionSettings settings, Assembly cmdletAssembly, Action<IServiceCollection> configureServices)
        {
            if (_provider is not null)
            {
                return _provider.CreateScope();
            }

            ServiceCollection services = new();
            configureServices(services);

            services
                //.AddClock(mock => mock.GetNow = c => c.Now.AddDays(-7d))
                .AddClock()
                .AddMemoryCache()
                .AddSingleton<Queue<IApiCmdlet>>()
                .AddSonarrClient(cmdletAssembly, settings, (provider, options) =>
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

            _provider = services.BuildServiceProvider(providerOptions);
            return _provider.CreateScope();
        }

        private static void AddObjectPools(IServiceCollection services)
        {
            services.AddObjectPoolReturner()
                    .AddGenericObjectPool<QueryParameterCollection>(builder =>
                    {
                        builder.SetConstructor(() => new QueryParameterCollection(10))
                               .SetDeconstructor(col =>
                               {
                                   col.Clear();
                                   return true;
                               });
                    });

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