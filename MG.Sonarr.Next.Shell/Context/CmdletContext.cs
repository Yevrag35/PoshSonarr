using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Json.Converters;
using MG.Sonarr.Next.Services.Json.Converters.Spans;
using MG.Sonarr.Next.Services.Json.Modifiers;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models.Calendar;
using MG.Sonarr.Next.Services.Models.Episodes;
using MG.Sonarr.Next.Services.Models.Qualities;
using MG.Sonarr.Next.Services.Models.Series;
using MG.Sonarr.Next.Services.Models.Tags;
using MG.Sonarr.Next.Shell.Cmdlets;
using MG.Sonarr.Next.Shell.Cmdlets.Connection;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Settings;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
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

        internal static void SetContext(this ConnectSonarrInstanceCmdlet cmdlet, ConnectionSettings settings)
        {
            SonarrContext.Initialize(settings);
        }
        internal static void UnsetContext(this DisconnectSonarrInstanceCmdlet cmdlet)
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

        internal static void Initialize(ConnectionSettings settings)
        {
            if (_provider is not null)
            {
                return;
            }

            SonarrJsonOptions jsonOptions = CreateSonarrJsonOptions();

            ServiceCollection services = new();
            services
                //.AddMemoryCache()
                .AddSingleton(jsonOptions)
                .AddSingleton<Queue<IApiCmdlet>>()
                .AddSonarrClient(settings);

            MetadataHandler.AddMetadata(services);

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
        }

        private static SonarrJsonOptions CreateSonarrJsonOptions()
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
                        }
                    );

                options.Converters.AddMany(
                    objCon,
                    new SonarrObjectConverter<CalendarObject>(objCon),
                    new SonarrObjectConverter<EpisodeObject>(objCon),
                    new SonarrObjectConverter<EpisodeFileObject>(objCon),
                    new SonarrObjectConverter<QualityProfileObject>(objCon),
                    new SeriesObjectConverter<SeriesObject>(objCon),
                    new SonarrObjectConverter<TagObject>(objCon),
                    new SeriesObjectConverter<AddSeriesObject>(objCon),
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