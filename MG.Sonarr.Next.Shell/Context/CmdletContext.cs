using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Json.Converters;
using MG.Sonarr.Next.Services.Json.Modifiers;
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
    internal static class CmdletContextExtensions
    {
        internal static IServiceScope CreateScope(this SonarrCmdletBase cmdlet)
        {
            return SonarrContext.GetProvider().CreateScope();
        }

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
                ObjectConverter objCon = new(
                        ignoreProps: new string[]
                        {
                            Constants.META_PROPERTY_NAME,
                        },
                        convertTypes: new KeyValuePair<string, Type>[]
                        {
                            new("Tags", typeof(SortedSet<int>)),
                            new("Genres", typeof(string[])),
                        }
                    );

                options.Converters.AddMany(
                    objCon,
                    new SonarrObjectConverter<TagObject>(objCon),
                    new SeriesObjectConverter(objCon),
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