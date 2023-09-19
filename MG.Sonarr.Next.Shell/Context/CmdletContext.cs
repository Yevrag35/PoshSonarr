using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Json.Converters;
using MG.Sonarr.Next.Shell.Cmdlets;
using MG.Sonarr.Next.Shell.Cmdlets.Connection;
using MG.Sonarr.Next.Shell.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;

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

        internal static void SetContext(this ConnectSonarrInstance cmdlet)
        {
            SonarrContext.Initialize(cmdlet);
        }
    }

    file static class SonarrContext
    {
        static IServiceProvider _provider = null!;

        internal static IServiceProvider GetProvider()
        {
            return _provider ?? throw new InvalidOperationException("The Sonarr context is not set.");
        }

        internal static void Initialize(ConnectSonarrInstance cmdlet)
        {
            if (_provider is not null)
            {
                return;
            }

            var jsonOptions = new SonarrJsonOptions(options =>
            {
                options.Converters.Add(
                    new PSObjectConverter(
                        ignoreProps: new string[]
                        {
                            Constants.META_PROPERTY_NAME,
                        },
                        convertTypes: new KeyValuePair<string, Type>[]
                        {
                            new("Tags", typeof(SortedSet<int>)),
                            new("Genres", typeof(string[])),
                        }
                    ));
                options.Converters.Add(new SonarrResponseConverter());
                options.PropertyNamingPolicy = null;
            });

            ServiceCollection services = new();
            services
                .AddMemoryCache()
                .AddSingleton(jsonOptions)
                .AddSingleton<Queue<IApiCmdlet>>()
                .AddSonarrClient(cmdlet.Settings, (msg) => cmdlet.WriteVerbose(msg.StatusCode.ToString()));

            _provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        }
    }
}