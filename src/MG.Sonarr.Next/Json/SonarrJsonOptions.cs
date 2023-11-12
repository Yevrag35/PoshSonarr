using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json.Converters;
using MG.Sonarr.Next.Json.Converters.Spans;
using MG.Sonarr.Next.Json.Modifiers;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Episodes;
using Microsoft.Extensions.DependencyInjection;
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
            this.ForSerializing = new(JsonSerializerDefaults.Web)
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            };

            this.ForDebugging = new(JsonSerializerDefaults.Web)
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
            };

            setupDeserializer(this.ForDeserializing);

            foreach (var conv in this.ForDeserializing.Converters)
            {
                this.ForSerializing.Converters.Add(conv);
                this.ForDebugging.Converters.Add(conv);
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
                    options.PropertyNamingPolicy = null;
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
                      .AddSpanConverters(new KeyValuePair<string, SpanConverter>[]
                      {
                            new("AirDate", doSpanConverter),
                            new("FirstAired", doSpanConverter),
                            new("AirTime", timeConverter),
                            new("Duration", timeSpanConverter),
                            new("ApiKey", alwaysStringConverter),
                            new("DownloadId", alwaysStringConverter),
                            new("ReleaseHash", alwaysStringConverter),
                            new("TorrentInfoHash", alwaysStringConverter),
                      });
            });

            options.Converters.AddMany(objCon, new PostCommandWriter(), new SonarrResponseConverter());

            IEnumerable<JsonConverter> sonarrConverters = ConstructSonarrObjectConverters(objCon);
            options.Converters.AddMany(sonarrConverters);
        }

        private static IEnumerable<JsonConverter> ConstructSonarrObjectConverters(ObjectConverter converter)
        {
            Type genericClassType = typeof(SonarrObjectConverter<>);
            object[] ctorArgs = new object[] { converter };
            Type[] typeParams = new Type[1];

            IEnumerable<Type> types = GetSonarrObjectTypes();

            foreach (Type resolvedConverterType in types)
            {
                typeParams[0] = resolvedConverterType;
                yield return ConstructConverter(genericClassType, typeParams, ctorArgs);
            }
        }

        private static JsonConverter ConstructConverter(Type genericClassType, Type[] typeParams, object[] activatorArgs)
        {
            Type constructedClassType = genericClassType.MakeGenericType(typeParams);
            JsonConverter constructed = (JsonConverter?)Activator.CreateInstance(constructedClassType, activatorArgs)
                ?? throw new InvalidOperationException($"Unable to construct {constructedClassType.GetTypeName()}.");

            return constructed;
        }

        private static IEnumerable<KeyValuePair<string, Type>> EnumerateConverterProperties()
        {
            yield return new("AirDate", typeof(DateOnly));
            yield return new("EpisodeNumbers", typeof(int[]));
            yield return new("Episodes", typeof(SortedSet<EpisodeObject>));
            yield return new("Genres", typeof(string[]));
            yield return new("Ignored", typeof(StringSet));
            yield return new("Preferred", typeof(StringKeyValueSet<int>));
            yield return new("Required", typeof(StringSet));
            yield return new("Tags", typeof(SortedSet<int>));
        }
        private static IEnumerable<KeyValuePair<string, string>> EnumerateGlobalReplaceNames()
        {
            yield return new("Monitored", "IsMonitored");
            yield return new("ChmodFolder", "CHMODFolder");
            yield return new("ChownGroup", "CHOWNGroup");
            yield return new("TvdbId", "TVDbId");
        }
        private static IEnumerable<string> EnumerateIgnoreProperties()
        {
            yield return Constants.META_PROPERTY_NAME;
            yield return Constants.PROPERTY_SHORT_OVERVIEW;
        }

        private static IEnumerable<Type> GetSonarrObjectTypes()
        {
            Type attType = typeof(SonarrObjectAttribute);
            Type sonarrObjType = typeof(SonarrObject);
            Assembly thisAss = attType.Assembly;

            IEnumerable<Type> types = thisAss.GetExportedTypes()
                .Where(x => x.IsClass 
                            && 
                            !x.IsAbstract
                            &&
                            x.IsDefined(attType)
                            &&
                            sonarrObjType.IsAssignableFrom(x));

            return types;
        }
    }
}
