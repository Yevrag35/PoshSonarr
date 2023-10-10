using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json.Converters.Spans;
using MG.Sonarr.Next.Json.Converters;
using MG.Sonarr.Next.Json.Modifiers;
using MG.Sonarr.Next.Models.Calendar;
using MG.Sonarr.Next.Models.Commands;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.Indexers;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Models.Qualities;
using MG.Sonarr.Next.Models.Releases;
using MG.Sonarr.Next.Models.RootFolders;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization.Metadata;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Json
{
    public interface ISonarrJsonOptions
    {
        JsonSerializerOptions GetForDebugging();
        JsonSerializerOptions GetForDeserializing();
        JsonSerializerOptions GetForSerializing();
    }

    internal sealed class SonarrJsonOptions : ISonarrJsonOptions
    {
        readonly JsonSerializerOptions _deserializer;
        readonly JsonSerializerOptions _debugSerializer;
        readonly JsonSerializerOptions _requestSerializer;

        public SonarrJsonOptions(Action<JsonSerializerOptions> setupDeserializer)
        {
            ArgumentNullException.ThrowIfNull(setupDeserializer);

            _deserializer = new(JsonSerializerDefaults.Web);
            _requestSerializer = new(JsonSerializerDefaults.Web)
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            };

            _debugSerializer = new(JsonSerializerDefaults.Web)
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
            };

            setupDeserializer(_deserializer);

            foreach (var conv in _deserializer.Converters)
            {
                _requestSerializer.Converters.Add(conv);
                _debugSerializer.Converters.Add(conv);
            }
        }

        public JsonSerializerOptions GetForDebugging()
        {
            return _debugSerializer;
        }
        public JsonSerializerOptions GetForDeserializing()
        {
            return _deserializer;
        }
        public JsonSerializerOptions GetForSerializing()
        {
            return _requestSerializer;
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
                    var resolver = provider.GetRequiredService<IMetadataResolver>();

                    var doSpanConverter = new DateOnlyConverter();
                    var timeConverter = new TimeOnlyConverter();
                    var timeSpanConverter = new TimeSpanConverter();

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
                                new("Duration", timeSpanConverter),
                            },
                            resolver
                        );

                    options.Converters.AddMany(
                        objCon,
                        new PostCommandWriter(),
                        new SonarrObjectConverter<AddSeriesObject>(objCon),
                        new SonarrObjectConverter<BackupObject>(objCon),
                        new SonarrObjectConverter<CalendarObject>(objCon),
                        new SonarrObjectConverter<CommandObject>(objCon),
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
                        new SonarrObjectConverter<SeriesObject>(objCon),
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

                    configureOptions(provider, options);
                }

                return new SonarrJsonOptions(newAction);
            });
        }
    }
}
