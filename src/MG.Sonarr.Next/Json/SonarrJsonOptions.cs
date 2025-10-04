using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json.Converters;
using MG.Sonarr.Next.Json.Converters.Spans;
using MG.Sonarr.Next.Json.Modifiers;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.Fields;
using MG.Sonarr.Next.Models.ManualImports;
using MG.Sonarr.Next.Reflection;
using MG.Sonarr.Resources;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace MG.Sonarr.Next.Json
{
    public interface ISonarrJsonOptions
    {
        JsonSerializerOptions ForDebugging { get; }
        JsonSerializerOptions ForDeserializing { get; }
        JsonSerializerOptions ForSerializing { get; }
    }

    internal sealed class SonarrJsonOptions : ISonarrJsonOptions
    {
        public JsonSerializerOptions ForDebugging { get; }
        public JsonSerializerOptions ForDeserializing { get; }
        public JsonSerializerOptions ForSerializing { get; }

        public SonarrJsonOptions(Action<JsonSerializerOptions> setupDeserializer)
        {
            ArgumentNullException.ThrowIfNull(setupDeserializer);

            this.ForDeserializing = new(JsonSerializerDefaults.Web);
            setupDeserializer(this.ForDeserializing);

            this.ForSerializing = new(this.ForDeserializing)
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
            };

            this.ForDebugging = new(this.ForDeserializing)
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };
        }

        [Obsolete("Complete rework", error: true)]
        internal static void ApplyCamelCasing(Span<char> chars)
        {
            for (int i = 0; i < chars.Length; i++)
            {
                if (i == 1 && !char.IsUpper(chars[i]))
                {
                    break;
                }

                bool hasNext = (i + 1 < chars.Length);

                // Stop when next char is already lowercase.
                if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
                {
                    // If the next char is a space, lowercase current char before exiting.
                    if (chars[i + 1] == ' ')
                    {
                        chars[i] = char.ToLowerInvariant(chars[i]);
                    }

                    break;
                }

                chars[i] = char.ToLowerInvariant(chars[i]);
            }
        }
    }

    public static class SonarrJsonDependencyInjection
    {
        public static IServiceCollection AddSonarrJsonOptions(this IServiceCollection services, Action<IServiceProvider, JsonSerializerOptions> configureOptions)
        {
            return services.AddSingleton<ISonarrJsonOptions>(provider =>
            {
                void newAction(JsonSerializerOptions options)
                {
                    options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                    options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
                    {
                        Modifiers =
                        {
                            JsonModifiers.AddPrivateFieldsModifier,
                        }
                    };

                    AddAllConverters(provider, options);

                    configureOptions(provider, options);
                }

                return new SonarrJsonOptions(newAction);
            });
        }

        private static void AddAllConverters(IServiceProvider provider, JsonSerializerOptions options)
        {
            IMetadataResolver resolver = provider.GetRequiredService<IMetadataResolver>();

            DateOnlyConverter doSpanConverter = new();
            TimeOnlyConverter timeConverter = new();
            TimeSpanConverter timeSpanConverter = new();
            AlwaysStringConverter alwaysStringConverter = new();

            ObjectConverter objCon = new(resolver, config =>
            {
                config.AddConvertProperties(EnumerateConverterProperties())
                      .AddGlobalReplaceNames(EnumerateGlobalReplaceNames())
                      .AddIgnoreProperties(EnumerateIgnoreProperties())
                      .AddSpanConverters(
                            new("AirDate", doSpanConverter),
                            new("Certification", alwaysStringConverter),
                            new("FirstAired", doSpanConverter),
                            new("AirTime", timeConverter),
                            new("Duration", timeSpanConverter),
                            new("ApiKey", alwaysStringConverter),
                            new("DownloadId", alwaysStringConverter),
                            new("ReleaseHash", alwaysStringConverter),
                            new("CleanTitle", alwaysStringConverter),
                            new("SortTitle", alwaysStringConverter),
                            new("Title", alwaysStringConverter),
                            new("TorrentInfoHash", alwaysStringConverter)
                      );
            });

            options.Converters.AddMany(
                objCon,
                new PostCommandWriter(),
                new SonarrResponseConverter(),
                new ImmutableArrayConverter<FieldObject>(),
                new ImmutableArrayConverter<ManualImportObject>(),
                new ImmutableArrayConverter<SelectOptionObject>());

            List<JsonConverter> sonarrConverters = ConstructSonarrObjectConverters(objCon);
            ThrowIfMissingConverters(sonarrConverters.Count == 0, sonarrConverters);

            options.Converters.AddMany(CollectionsMarshal.AsSpan(sonarrConverters));
        }

        private static List<JsonConverter> ConstructSonarrObjectConverters(ObjectConverter converter)
        {
            List<JsonConverter> output = new(10);
            Type genericClassType = typeof(SonarrObjectConverter<>);
            object[] ctorArgs = [converter];
            Type[] typeParams = new Type[1];

            IEnumerable<Type> types = GetSonarrObjectTypes();

            foreach (Type resolvedConverterType in types)
            {
                typeParams[0] = resolvedConverterType;
                JsonConverter constructed = ConstructConverter(genericClassType, typeParams, ctorArgs);
                output.Add(constructed);
            }

            return output;
        }

        private static JsonConverter ConstructConverter(Type genericClassType, Type[] typeParams, object[] activatorArgs)
        {
            Type constructedClassType = genericClassType.MakeGenericType(typeParams);
            JsonConverter constructed = (JsonConverter?)Activator.CreateInstance(constructedClassType, activatorArgs)
                ?? throw new InvalidOperationException($"Unable to construct {constructedClassType.GetName()}.");

            return constructed;
        }

        private static KeyValuePair<string, Type>[] EnumerateConverterProperties()
        {
             return [
                 new("AirDate", typeof(DateOnly)),
                 new("EpisodeNumbers", typeof(int[])),
                 new("Episodes", typeof(SortedSet<EpisodeObject>)),
                 new("Fields", typeof(ImmutableArray<FieldObject>)),
                 new("Genres", typeof(string[])),
                 new("Ignored", typeof(StringSet)),
                 new("Preferred", typeof(StringKeyValueSet<int>)),
                 new("ReleaseGroups", typeof(string[])),
                 new("Required", typeof(StringSet)),
                 new("SelectOptions", typeof(ImmutableArray<SelectOptionObject>)),
                 new("Tags", typeof(SortedSet<int>)),
             ];
        }
        private static KeyValuePair<string, string>[] EnumerateGlobalReplaceNames()
        {
             return [
                 new("Monitored", "IsMonitored"),
                 new("ChmodFolder", "CHMODFolder"),
                 new("ChownGroup", "CHOWNGroup"),
                 new("TvdbId", "TVDbId"),
             ];
        }
        private static string[] EnumerateIgnoreProperties()
        {
             return [
                 Constants.META_PROPERTY_NAME,
                 Constants.PROPERTY_SHORT_OVERVIEW,
             ];
        }

        private static IEnumerable<Type> GetSonarrObjectTypes()
        {
            Assembly[] assemblies = AssemblyLoader.GetAppDomainAssemblies(AppDomain.CurrentDomain);

            return assemblies
                .Where(ass => !ass.IsDynamic 
                            && ass.IsDefined(
                                    typeof(SonarrObjectConverterAssemblyAttribute),
                                    inherit: false))

                .SelectMany(ass => ass.GetExportedTypes()
                                      .Where(t => t.IsClass
                                                  &&
                                                  !t.IsAbstract
                                                  &&
                                                  t.IsDefined(
                                                      typeof(SonarrObjectAttribute),
                                                      inherit: false)
                                                  &&
                                                  typeof(SonarrObject).IsAssignableFrom(t)));
        }

        /// <exception cref="ModuleStartupException"></exception>
        private static void ThrowIfMissingConverters([DoesNotReturnIf(true)] bool condition, List<JsonConverter> offender, [CallerArgumentExpression(nameof(offender))] string? paramName = null)
        {
            if (condition)
            {
                ArgumentOutOfRangeException countEx = new(paramName, offender.Count, "Expected one or more converters in the list.");
                throw new ModuleStartupException(
                    message: ExMessages.Startup_Exception_NoConvertersConstructed,
                    offendingType: typeof(SonarrJsonDependencyInjection),
                    innerException: countEx);
            }
        }
    }
}
