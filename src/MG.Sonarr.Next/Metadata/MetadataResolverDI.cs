using MG.Sonarr.Next.Attributes;

namespace MG.Sonarr.Next.Metadata;

/// <summary>
/// Provides the extension method that registers the metadata tags of every Sonarr API endpoint that the module uses.
/// </summary>
public static class MetadataResolverDI
{
	/// <summary>
	/// The resolver that <see cref="CreateResolver(Assembly)"/> created for each cmdlet assembly, which every connection
	/// registers.
	/// </summary>
	/// <remarks>
	/// The table holds each assembly weakly, so it keeps none loaded, and it's thread-safe. When two threads look up a
	/// new assembly at once, each can create a resolver, but the table keeps one and returns it to both.
	/// </remarks>
	private static readonly ConditionalWeakTable<Assembly, MetadataResolver> _resolvers = new();

	/// <summary>
	/// Registers an <see cref="IMetadataResolver"/> singleton that holds a tag for each endpoint, with the cmdlets that
	/// each tag's objects can be piped to.
	/// </summary>
	/// <remarks>
	/// <para>Each tag's name comes from <see cref="Meta"/>, its path from <see cref="Constants"/>, and its cmdlets from
	/// the <see cref="MetadataCanPipeAttribute"/> attributes on the cmdlet classes in
	/// <paramref name="cmdletAssembly"/>.</para>
	/// <para>The first call for an assembly creates the resolver, and every later call for it registers that same
	/// instance, so every connection in the process shares one resolver. The resolver can't change once it's created,
	/// so any number of threads can read it at once.</para>
	/// </remarks>
	/// <param name="services">The service collection to add the resolver to.</param>
	/// <param name="cmdletAssembly">The assembly whose cmdlet classes are searched for pipe targets.</param>
	/// <returns><paramref name="services"/>, so that calls can be chained.</returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="services"/> or <paramref name="cmdletAssembly"/> is null.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// Thrown when a cmdlet class with a <see cref="MetadataCanPipeAttribute"/> has no <see cref="CmdletAttribute"/>
	/// that gives its verb and noun.
	/// </exception>
	public static IServiceCollection AddMetadata(this IServiceCollection services, Assembly cmdletAssembly)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(cmdletAssembly);

		MetadataResolver resolver = _resolvers.GetValue(cmdletAssembly, CreateResolver);
		return services.AddSingleton<IMetadataResolver>(resolver);
	}

	/// <summary>
	/// Creates the resolver that holds a tag for each endpoint, with the cmdlets in an assembly that each tag's objects
	/// can be piped to.
	/// </summary>
	/// <param name="cmdletAssembly">The assembly whose cmdlet classes are searched for pipe targets.</param>
	/// <returns>The frozen resolver of every endpoint's tag.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when a cmdlet class with a <see cref="MetadataCanPipeAttribute"/> has no <see cref="CmdletAttribute"/>
	/// that gives its verb and noun.
	/// </exception>
	private static MetadataResolver CreateResolver(Assembly cmdletAssembly)
	{
		int initialCapacity = 35;
		var pipes = FindPipeableCmdlets(cmdletAssembly);
		MetadataResolver.Builder builder = new(initialCapacity, pipes)
		{
			{ Meta.Backup, Constants.Backup, true },
			{ Meta.Calendar, Constants.Calendar, false },
			{ Meta.Command, Constants.Command, true },
			{ Meta.DelayProfile, Constants.DelayProfile, true },
			{ Meta.Disk, Constants.DiskSpace, false },
			{ Meta.DownloadClient, Constants.DownloadClient, true },
			{ Meta.DownloadClientConfig, Constants.DownloadClientConfig, true },
			{ Meta.DownloadClientSchema, Constants.DownloadClientSchema, false },
			{ Meta.Episode, Constants.Episode, true },
			{ Meta.EpisodeFile, Constants.EpisodeFile, true },
			{ Meta.History, Constants.History, false },
			{ Meta.HistorySince, Constants.HistorySince, false },
			{ Meta.HostConfig, Constants.Host, true },
			{ Meta.Indexer, Constants.Indexer, true },
			{ Meta.LanguageProfile, Constants.LanguageProfile, true },
			{ Meta.LogItem, Constants.Log, false },
			{ Meta.LogFile, Constants.LogFile, false },
			{ Meta.ManualImport, Constants.ManualImport, false },
			{ Meta.MediaManagement, Constants.MediaManagement, true },
			{ Meta.NamingConfig, Constants.NamingConfig, true },
			{ Meta.Notification, Constants.Notification, true },
			{ Meta.Quality, Constants.QualityDefinitions, false },
			{ Meta.QualityDefinition, Constants.QualityDefinitions, true },
			{ Meta.QualityProfile, Constants.QualityProfile, true },
			{ Meta.Release, Constants.Release, false },
			{ Meta.ReleaseProfile, Constants.ReleaseProfile, true },
			{ Meta.RemotePathMapping, Constants.RemotePath, true },
			{ Meta.Renamable, Constants.Rename, false },
			{ Meta.QualityRevision, Constants.QualityDefinitions, false },
			{ Meta.RootFolder, Constants.RootFolder, true },
			{ Meta.Series, Constants.Series, true },
			{ Meta.SeriesAdd, Constants.SeriesLookup, false },
			{ Meta.SeriesHistory, Constants.HistoryBySeries, false },
			{ Meta.Status, Constants.Status, false },
			{ Meta.Tag, Constants.Tag, true },
		};

		Debug.Assert(builder.Count <= initialCapacity);
		return builder.Freeze();
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
