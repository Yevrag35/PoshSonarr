using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json.Converters;
using MG.Sonarr.Next.Json.Converters.Spans;
using MG.Sonarr.Next.Json.Modifiers;
using MG.Sonarr.Next.Json.Naming;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.Fields;
using MG.Sonarr.Next.Models.ManualImports;
using MG.Sonarr.Next.Reflection;
using MG.Sonarr.Resources;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace MG.Sonarr.Next.Json;

/// <summary>
/// Exposes configured JSON serializer option sets used by Sonarr operations.
/// </summary>
public interface ISonarrJsonOptions
{
	/// <summary>
	/// Gets options used for debug-friendly JSON output.
	/// </summary>
	/// <value>The serializer options for debugging output.</value>
	JsonSerializerOptions ForDebugging { get; }

	/// <summary>
	/// Gets options used for JSON deserialization.
	/// </summary>
	/// <value>The serializer options for deserialization.</value>
	JsonSerializerOptions ForDeserializing { get; }

	/// <summary>
	/// Gets options used for JSON serialization.
	/// </summary>
	/// <value>The serializer options for serialization.</value>
	JsonSerializerOptions ForSerializing { get; }
}

/// <summary>
/// Represents the default implementation of <see cref="ISonarrJsonOptions"/>.
/// </summary>
internal sealed class SonarrJsonOptions : ISonarrJsonOptions
{
	/// <inheritdoc cref="ISonarrJsonOptions.ForDebugging"/>
	public JsonSerializerOptions ForDebugging { get; }

	/// <inheritdoc cref="ISonarrJsonOptions.ForDeserializing"/>
	public JsonSerializerOptions ForDeserializing { get; }

	/// <inheritdoc cref="ISonarrJsonOptions.ForSerializing"/>
	public JsonSerializerOptions ForSerializing { get; }

	/// <summary>
	/// Initializes a new <see cref="SonarrJsonOptions"/> instance.
	/// </summary>
	/// <param name="setupDeserializer">The callback that configures deserialization options.</param>
	/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="setupDeserializer"/> is null.</exception>
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

	/// <summary>
	/// Applies camel-casing to a character span.
	/// </summary>
	/// <param name="chars">The characters to transform.</param>
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

/// <summary>
/// Provides dependency injection registrations for Sonarr JSON services.
/// </summary>
public static class SonarrJsonDependencyInjection
{
	/// <summary>
	/// Adds Sonarr JSON options services to the specified service collection.
	/// </summary>
	/// <param name="services">The service collection to update.</param>
	/// <param name="configureOptions">The callback that configures serializer options.</param>
	/// <returns>The same <see cref="IServiceCollection"/> instance so additional calls can be chained.</returns>
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

	/// <summary>
	/// Adds all Sonarr JSON converters to the specified serializer options.
	/// </summary>
	/// <param name="provider">The service provider used to resolve dependencies.</param>
	/// <param name="options">The serializer options to update.</param>
	private static void AddAllConverters(IServiceProvider provider, JsonSerializerOptions options)
	{
		IMetadataResolver resolver = provider.GetRequiredService<IMetadataResolver>();
		WorkingNamingPolicy policy = new(options);

		ObjectConverter objCon = new(resolver, policy, config =>
		{
			DateOnlyConverter doSpanConverter = new();
			TimeOnlyConverter timeConverter = new();
			TimeSpanConverter timeSpanConverter = new();
			AlwaysStringConverter alwaysStringConverter = new();
			AlwaysStringConverter exactStringConverter = new(trim: false);

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
						new("TorrentInfoHash", alwaysStringConverter),
						new("Label", alwaysStringConverter),
						// Keyed by the names that EnumerateGlobalReplaceNames gives these properties.
						new("CHMODFolder", alwaysStringConverter),
						new("CHOWNGroup", alwaysStringConverter),
						// Credentials go back to Sonarr exactly as they came, whitespace included.
						new("Username", exactStringConverter),
						new("Password", exactStringConverter),
						new("PasswordConfirmation", exactStringConverter),
						new("SslCertPassword", exactStringConverter),
						new("ProxyUsername", exactStringConverter),
						new("ProxyPassword", exactStringConverter)
				  );
		});

		options.Converters.AddMany(
			objCon,
			new PostCommandWriter(policy),
			new SonarrResponseConverter(policy),
			new ImmutableArrayConverter<FieldObject>(),
			new ImmutableArrayConverter<ManualImportObject>(),
			new ImmutableArrayConverter<SelectOptionObject>());

		List<JsonConverter> sonarrConverters = ConstructSonarrObjectConverters(objCon);
		ThrowIfMissingConverters(sonarrConverters.Count == 0, sonarrConverters);

		options.Converters.AddMany(CollectionsMarshal.AsSpan(sonarrConverters));
	}

	/// <summary>
	/// Constructs all Sonarr object converters from discovered Sonarr object types.
	/// </summary>
	/// <param name="converter">The shared object converter dependency.</param>
	/// <returns>A list of constructed Sonarr object converters.</returns>
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

	/// <summary>
	/// Constructs a closed generic JSON converter instance.
	/// </summary>
	/// <param name="genericClassType">The open generic converter type.</param>
	/// <param name="typeParams">The type arguments for the generic converter type.</param>
	/// <param name="activatorArgs">The constructor arguments for the converter instance.</param>
	/// <returns>The constructed converter instance.</returns>
	/// <exception cref="System.InvalidOperationException">Thrown when the converter instance cannot be created.</exception>
	private static JsonConverter ConstructConverter(Type genericClassType, Type[] typeParams, object[] activatorArgs)
	{
		Type constructedClassType = genericClassType.MakeGenericType(typeParams);
		JsonConverter constructed = (JsonConverter?)Activator.CreateInstance(constructedClassType, activatorArgs)
			?? throw new InvalidOperationException($"Unable to construct {constructedClassType.GetName()}.");

		return constructed;
	}

	/// <summary>
	/// Enumerates property-type pairs that use converter-specific property handling.
	/// </summary>
	/// <returns>The configured converter property mappings.</returns>
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

	/// <summary>
	/// Enumerates global property name replacements used during object conversion.
	/// </summary>
	/// <returns>The configured replacement mappings.</returns>
	private static KeyValuePair<string, string>[] EnumerateGlobalReplaceNames()
	{
		return [
			new("Monitored", "IsMonitored"),
			 new("ChmodFolder", "CHMODFolder"),
			 new("ChownGroup", "CHOWNGroup"),
			 new("TvdbId", "TVDbId"),
		 ];
	}

	/// <summary>
	/// Enumerates property names ignored by object conversion.
	/// </summary>
	/// <returns>The ignored property names.</returns>
	private static string[] EnumerateIgnoreProperties()
	{
		return [
			Constants.META_PROPERTY_NAME,
			 Constants.PROPERTY_SHORT_OVERVIEW,
		 ];
	}

	/// <summary>
	/// Gets exported Sonarr object types from marked assemblies.
	/// </summary>
	/// <returns>The discovered Sonarr object types.</returns>
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

	/// <summary>
	/// Throws when no converters were constructed.
	/// </summary>
	/// <param name="condition"><see langword="true"/> to throw; otherwise, <see langword="false"/>.</param>
	/// <param name="offender">The converter list used for count reporting.</param>
	/// <param name="paramName">The argument name used for exception reporting.</param>
	/// <exception cref="ModuleStartupException">Thrown when <paramref name="condition"/> is <see langword="true"/>.</exception>
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
