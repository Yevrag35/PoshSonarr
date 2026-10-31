using MG.Sonarr.Next.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Management.Automation;

namespace MG.Sonarr.Next.Metadata;

/// <summary>
/// Provides the extension method that registers the metadata tags of every Sonarr API endpoint that the module uses.
/// </summary>
public static class MetadataResolverDI
{
	/// <summary>
	/// Registers an <see cref="IMetadataResolver"/> singleton that holds a tag for each endpoint, with the cmdlets that
	/// each tag's objects can be piped to.
	/// </summary>
	/// <remarks>
	/// Each tag's name comes from <see cref="Meta"/>, its path from <see cref="Constants"/>, and its cmdlets from the
	/// <see cref="MetadataCanPipeAttribute"/> attributes on the cmdlet classes in <paramref name="cmdletAssembly"/>.
	/// </remarks>
	/// <param name="services">The service collection to add the resolver to.</param>
	/// <param name="cmdletAssembly">The assembly whose cmdlet classes are searched for pipe targets.</param>
	/// <returns><paramref name="services"/>, so that calls can be chained.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a cmdlet class with a <see cref="MetadataCanPipeAttribute"/> has no <see cref="CmdletAttribute"/>
	/// that gives its verb and noun.
	/// </exception>
	public static IServiceCollection AddMetadata(this IServiceCollection services, Assembly cmdletAssembly)
	{
		int initialCapacity = 35;
		var pipes = FindPipeableCmdlets(cmdletAssembly);
		MetadataResolver dict = new(initialCapacity, pipes)
		{
			{ Meta.BACKUP, Constants.BACKUP, true },
			{ Meta.CALENDAR, Constants.CALENDAR, false },
			{ Meta.COMMAND, Constants.COMMAND, true },
			{ Meta.DELAY_PROFILE, Constants.DELAY_PROFILE, true },
			{ Meta.DISK, Constants.DISKSPACE, false },
			{ Meta.DOWNLOAD_CLIENT, Constants.DOWNLOAD_CLIENT, true },
			{ Meta.DOWNLOAD_CLIENT_CONFIG, Constants.DOWNLOAD_CLIENT_CONFIG, true },
			{ Meta.DOWNLOAD_CLIENT_SCHEMA, Constants.DOWNLOAD_CLIENT_SCHEMA, false },
			{ Meta.EPISODE, Constants.EPISODE, true },
			{ Meta.EPISODE_FILE, Constants.EPISODEFILE, true },
			{ Meta.HISTORY, Constants.HISTORY, false },
			{ Meta.HISTORY_SINCE, Constants.HISTORY_SINCE, false },
			{ Meta.HOST, Constants.HOST, true },
			{ Meta.INDEXER, Constants.INDEXER, true },
			{ Meta.LANGUAGE, Constants.LANGUAGE_PROFILE, true },
			{ Meta.LOG_ITEM, Constants.LOG, false },
			{ Meta.LOG_FILE, Constants.LOGFILE, false },
			{ Meta.MANUAL_IMPORT, Constants.MANUAL_IMPORT, false },
			{ Meta.MEDIA_MANGEMENT, Constants.MEDIA_MANAGEMENT, true },
			{ Meta.NAMING_CONFIG, Constants.NAMING_CONFIG, true },
			{ Meta.NOTIFICATION, Constants.NOTIFICATION, true },
			{ Meta.QUALITY, Constants.QUALITY_DEFINITIONS, false },
			{ Meta.QUALITY_DEFINITION, Constants.QUALITY_DEFINITIONS, true },
			{ Meta.QUALITY_PROFILE, Constants.PROFILE, true },
			{ Meta.RELEASE, Constants.RELEASE, false },
			{ Meta.RELEASE_PROFILE, Constants.RELEASE_PROFILE, true },
			{ Meta.REMOTE_PATH_MAPPING, Constants.REMOTE_PATH, true },
			{ Meta.RENAMABLE, Constants.RENAME, false },
			{ Meta.REVISION, Constants.QUALITY_DEFINITIONS, false },
			{ Meta.ROOT_FOLDER, Constants.ROOTFOLDER, true },
			{ Meta.SERIES, Constants.SERIES, true },
			{ Meta.SERIES_ADD, Constants.SERIES_LOOKUP, false },
			{ Meta.SERIES_HISTORY, Constants.HISTORY_BY_SERIES, false },
			{ Meta.STATUS, Constants.STATUS, false },
			{ Meta.TAG, Constants.TAG, true },
		};

		Debug.Assert(dict.Count <= initialCapacity);
		return services.AddSingleton<IMetadataResolver>(dict);
	}

	/// <summary>
	/// Finds the cmdlets that each tag's objects can be piped to, from the attributes of an assembly's cmdlet classes.
	/// </summary>
	/// <remarks>
	/// A cmdlet counts when it's a public, non-abstract class with its own <see cref="CmdletAttribute"/> and at least
	/// one <see cref="MetadataCanPipeAttribute"/>. Its name is formed as <c>Verb-Noun</c>.
	/// </remarks>
	/// <param name="cmdletAssembly">The assembly to search.</param>
	/// <returns>
	/// The cmdlet names for each tag, in ordinal order, keyed by tag name without regard to case.
	/// </returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a cmdlet class has no <see cref="CmdletAttribute"/> that gives its verb and noun.
	/// </exception>
	internal static Dictionary<string, ImmutableArray<string>> FindPipeableCmdlets(Assembly cmdletAssembly)
	{
		IEnumerable<Type> cmdletTypes = cmdletAssembly.GetExportedTypes()
			.Where(x => x.IsPublic
					 && x.IsClass
					 && !x.IsAbstract
					 && x.IsDefined(typeof(CmdletAttribute), inherit: false)
					 && x.IsDefined(typeof(MetadataCanPipeAttribute), inherit: false));

		return cmdletTypes
			.SelectMany(type =>
			{
				string cmdletName = GetCmdletNameFromAttribute(type, out int howManyMetaAtts);
				return GetTagAndCmdletNamePair(type, cmdletName, howManyMetaAtts);
			})
			.GroupBy(x => x.First)
			.ToDictionary(
				keySelector: x => x.Key,
				elementSelector: x => x.Select(c => c.Second).Order(StringComparer.Ordinal).ToImmutableArray(),
				comparer: StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Gets a cmdlet's name from its <see cref="CmdletAttribute"/> and counts its
	/// <see cref="MetadataCanPipeAttribute"/> attributes.
	/// </summary>
	/// <param name="cmdletType">The cmdlet class.</param>
	/// <param name="howManyMetaAtts">
	/// When this method returns, the number of <see cref="MetadataCanPipeAttribute"/> attributes on
	/// <paramref name="cmdletType"/>.
	/// </param>
	/// <returns>The cmdlet's name, as <c>Verb-Noun</c>.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="cmdletType"/> has no <see cref="CmdletAttribute"/> that gives its verb and noun.
	/// </exception>
	private static string GetCmdletNameFromAttribute(Type cmdletType, out int howManyMetaAtts)
	{
		TwoStrings verbAndNoun = GetVerbAndNoun(cmdletType, out howManyMetaAtts);
		return string.Concat(verbAndNoun.First, ['-'], verbAndNoun.Second);
	}

	/// <summary>
	/// Pairs each tag that a cmdlet's <see cref="MetadataCanPipeAttribute"/> attributes name with the cmdlet's name.
	/// </summary>
	/// <param name="cmdletType">The cmdlet class.</param>
	/// <param name="cmdletName">The cmdlet's name.</param>
	/// <param name="howManyMetaApps">The number of <see cref="MetadataCanPipeAttribute"/> attributes on
	/// <paramref name="cmdletType"/>.</param>
	/// <returns>An array of pairs, each with a tag name first and the cmdlet's name second.</returns>
	/// <exception cref="InvalidOperationException">Thrown when an attribute's tag isn't a string.</exception>
	private static TwoStrings[] GetTagAndCmdletNamePair(Type cmdletType, string cmdletName, int howManyMetaApps)
	{
		Debug.Assert(howManyMetaApps > 0, "Should have at least one MetadataCanPipeAttribute.");

		TwoStrings[] array = new TwoStrings[howManyMetaApps];
		int i = 0;

		foreach (CustomAttributeData cad in cmdletType.CustomAttributes)
		{
			if (i >= howManyMetaApps)
				break;

			if (typeof(MetadataCanPipeAttribute).Equals(cad.AttributeType))
			{
				array[i++] = new TwoStrings(GetMetadataTag(cad.NamedArguments), cmdletName);
			}
		}

		Debug.Assert(i == howManyMetaApps, "Should have found the expected number of MetadataCanPipeAttributes.");
		return array;
	}

	/// <summary>
	/// Gets the tag name that a <see cref="MetadataCanPipeAttribute"/> sets, from its first named argument.
	/// </summary>
	/// <param name="constructorArgs">The attribute's named arguments, whose first is its <c>Tag</c>.</param>
	/// <returns>The tag's name.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the first named argument's value is null.</exception>
	/// <exception cref="InvalidCastException">Thrown when the first named argument's value isn't a string.</exception>
	private static string GetMetadataTag(IList<CustomAttributeNamedArgument> constructorArgs)
	{
		return (string?)constructorArgs[0].TypedValue.Value ?? throw new InvalidOperationException("Expected a string tag.");
	}

	/// <summary>
	/// Gets the verb and noun from a cmdlet's <see cref="CmdletAttribute"/> and counts its
	/// <see cref="MetadataCanPipeAttribute"/> attributes.
	/// </summary>
	/// <param name="cmdletType">The cmdlet class, which has a <see cref="CmdletAttribute"/> whose first two constructor
	/// arguments are strings.</param>
	/// <param name="howManyPipeAtts">
	/// When this method returns, the number of <see cref="MetadataCanPipeAttribute"/> attributes on
	/// <paramref name="cmdletType"/>.
	/// </param>
	/// <returns>A pair with the verb first and the noun second.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="cmdletType"/> has no <see cref="CmdletAttribute"/> whose first two constructor
	/// arguments are strings.
	/// </exception>
	private static TwoStrings GetVerbAndNoun(Type cmdletType, out int howManyPipeAtts)
	{
		TwoStrings combo = default;
		bool hasCombo = false;
		howManyPipeAtts = 0;

		foreach (CustomAttributeData cad in cmdletType.CustomAttributes)
		{
			if (typeof(MetadataCanPipeAttribute).Equals(cad.AttributeType))
			{
				howManyPipeAtts++;
				continue;
			}

			if (typeof(CmdletAttribute).Equals(cad.AttributeType) && TryGetVerbAndNoun(cad.ConstructorArguments, out string? verb, out string? noun))
			{
				combo = new(verb, noun);
				hasCombo = hasCombo = true;
			}
		}

		return hasCombo
			? combo
			: throw new ArgumentException($"{cmdletType} does not have the right CmdletAttribute constructor signature.", nameof(cmdletType));
	}

	/// <summary>
	/// Reads the verb and noun from a <see cref="CmdletAttribute"/>'s constructor arguments.
	/// </summary>
	/// <param name="constructorArgs">The attribute's constructor arguments.</param>
	/// <param name="verb">When this method returns <see langword="true"/>, the cmdlet's verb; otherwise,
	/// <see langword="null"/>.</param>
	/// <param name="noun">When this method returns <see langword="true"/>, the cmdlet's noun; otherwise,
	/// <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if the first two arguments are strings; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool TryGetVerbAndNoun(
		IList<CustomAttributeTypedArgument> constructorArgs,
		[NotNullWhen(true)] out string? verb,
		[NotNullWhen(true)] out string? noun)
	{
		if (constructorArgs.Count < 2
			|| constructorArgs[0].Value is not string v
			|| constructorArgs[1].Value is not string n)
		{
			verb = null;
			noun = null;
			return false;
		}

		verb = v;
		noun = n;
		return true;
	}

	/// <summary>
	/// Represents a pair of strings, such as a verb and a noun, or a tag name and a cmdlet name.
	/// </summary>
	[StructLayout(LayoutKind.Auto)]
	private readonly struct TwoStrings
	{
		/// <summary>
		/// The first string of the pair, never null.
		/// </summary>
		public readonly string First;
		/// <summary>
		/// The second string of the pair, never null.
		/// </summary>
		public readonly string Second;
		/// <summary>
		/// Initializes a new <see cref="TwoStrings"/> instance with the specified strings.
		/// </summary>
		/// <param name="first">The first string, or <see langword="null"/> for an empty string.</param>
		/// <param name="second">The second string, or <see langword="null"/> for an empty string.</param>
		internal TwoStrings(string first, string second)
		{
			First = first ?? string.Empty;
			Second = second ?? string.Empty;
		}
	}
}
