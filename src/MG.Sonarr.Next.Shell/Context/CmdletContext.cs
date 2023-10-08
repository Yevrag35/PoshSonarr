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
        public static MetadataResolver GetResolver()
        {
            return SonarrContext.GetProvider().GetRequiredService<MetadataResolver>();
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
                .AddSonarrClient(settings);

            AddObjectPools(services);
            var resolver = MetadataHandler.AddMetadata(services);
            SonarrJsonOptions jsonOptions = CreateSonarrJsonOptions(resolver);
            services.AddSingleton(jsonOptions);

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
        private static SonarrJsonOptions CreateSonarrJsonOptions(MetadataResolver resolver)
        {
            return new SonarrJsonOptions(options =>
            {
                var doSpanConverter = new DateOnlyConverter();
                var timeConverter = new TimeOnlyConverter();

                ObjectConverter objCon = new(
                        ignoreProps: new string[]
                        {
                            Constants.META_PROPERTY_NAME,
                        },
                        replaceNames: new KeyValuePair<string, string>[]
                        {
                            new("Monitored", "IsMonitored"),
                            new("TvdbId", "TVDbId"),
                        },
                        convertTypes: new KeyValuePair<string, Type>[]
                        {
                            new("AirDate", typeof(DateOnly)),
                            new("Tags", typeof(SortedSet<int>)),
                            new("Genres", typeof(string[])),
                        },
                        spanConverters: new KeyValuePair<string, SpanConverter>[]
                        {
                            new("AirDate", doSpanConverter),
                            new("FirstAired", doSpanConverter),
                            new("AirTime", timeConverter),
                        },
                        resolver
                    );

                options.Converters.AddMany(
                    objCon,
                    new BackupObjectConverter(objCon),
                    new SeriesObjectConverter<AddSeriesObject>(objCon),
                    new SonarrObjectConverter<CalendarObject>(objCon),
                    new SonarrObjectConverter<DelayProfileObject>(objCon),
                    new SonarrObjectConverter<EpisodeObject>(objCon),
                    new SonarrObjectConverter<EpisodeFileObject>(objCon),
                    new SonarrObjectConverter<IndexerObject>(objCon),
                    new SonarrObjectConverter<LanguageProfileObject>(objCon),
                    new SonarrObjectConverter<LogObject>(objCon),
                    new SonarrObjectConverter<LogFileObject>(objCon),
                    new SonarrObjectConverter<QualityDefinitionObject>(objCon),
                    new SonarrObjectConverter<QualityProfileObject>(objCon),
                    new SonarrObjectConverter<ReleaseObject>(objCon),
                    new SonarrObjectConverter<ReleaseProfileObject>(objCon),
                    new SonarrObjectConverter<RootFolderObject>(objCon),
                    new SeriesObjectConverter<SeriesObject>(objCon),
                    new SonarrObjectConverter<SonarrServerError>(objCon),
                    new SonarrObjectConverter<TagObject>(objCon),
                    new SonarrResponseConverter());

                options.PropertyNamingPolicy = null;
                options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers =
                    {
                        JsonModifiers.AddPrivateFieldsModifier,
                    }
                };
            });
        }
    }
}