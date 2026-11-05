using MG.Sonarr.Next.Shell.Components;
using System.Buffers;
using System.Collections;
using System.Collections.Immutable;
using System.Management.Automation.Language;

namespace MG.Sonarr.Next.Shell.Completers;

/// <summary>
/// Provides the argument completer of <c>Get-SonarrHistory</c>'s <c>-EventType</c> parameter, which offers the names of
/// the kinds of event that Sonarr records in an episode's history, and converts a name to the number that Sonarr takes.
/// </summary>
/// <remarks>
/// <para>
/// For an empty argument, the completer offers every name. For any other, it offers the names that start with the
/// argument, without its leading and trailing white space, ignoring case, and the argument can hold the <c>*</c> and
/// <c>?</c> wildcards. The names come in ordinal order, each as a parameter value whose list text and tooltip are the
/// name.
/// </para>
/// <para>
/// When no name matches, the completer returns an empty collection rather than <see langword="null"/>, so PowerShell
/// offers nothing instead of file names.
/// </para>
/// <para><b>Performance:</b> The names and the map from names to numbers are built once, when the type is first used.
/// A completion rents its results' array from the shared pool and returns it cleared, and builds each match's pattern
/// on the stack.</para>
/// </remarks>
internal sealed class EventTypeCompleter : IArgumentCompleter
{
	/// <summary>
	/// Represents a method that determines whether a value matches a wildcard pattern.
	/// </summary>
	/// <remarks>
	/// <para>TODO: Nothing uses this delegate. Is it left over from an earlier way of matching names, or meant for
	/// one to come?</para>
	/// </remarks>
	/// <param name="matcher">The matcher that holds the pattern.</param>
	/// <param name="value">The value to match against the pattern.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="value"/> matches the pattern; otherwise, <see langword="false"/>.
	/// </returns>
	private delegate bool PatternPredicate(in PatternMatcher matcher, ReadOnlySpan<char> value);
	private static readonly ImmutableDictionary<string, int> _namesToInts;
	private static readonly string[] _names;
	/// <summary>
	/// Initializes the <see cref="EventTypeCompleter"/> type's sorted event type names and its map from names to
	/// numbers.
	/// </summary>
	static EventTypeCompleter()
	{
		_namesToInts = GetTypeDictionary(out _names);
	}

	/// <summary>
	/// Initializes a new <see cref="EventTypeCompleter"/> instance.
	/// </summary>
	/// <remarks>
	/// The completer holds no state of its own: every instance shares the type's names.
	/// </remarks>
	public EventTypeCompleter() { }

	/// <summary>
	/// Gets the completions for an argument of <c>-EventType</c>: the event type names that start with it.
	/// </summary>
	/// <param name="commandName">
	/// The name of the command whose argument is completed, which the completer doesn't use.
	/// </param>
	/// <param name="parameterName">
	/// The name of the parameter whose argument is completed, which the completer doesn't use.
	/// </param>
	/// <param name="wordToComplete">
	/// The argument's whole text, or, in an unquoted list, the part after the last comma.
	/// </param>
	/// <param name="commandAst">The syntax tree of the command, which the completer doesn't use.</param>
	/// <param name="fakeBoundParameters">The parameters bound so far, which the completer doesn't use.</param>
	/// <returns>
	/// The completions for the names that match <paramref name="wordToComplete"/>, in ordinal order, or an empty array
	/// when none matches.
	/// </returns>
	public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
	{
		CompletionResult[] array = ArrayPool<CompletionResult>.Shared.Rent(_names.Length);
		try
		{
			bool empty = string.IsNullOrEmpty(wordToComplete);
			int count = 0;

			foreach (string name in _names)
			{
				if (empty || NameIsMatch(name, wordToComplete))
				{
					int value = _namesToInts[name];
					array[count++] = new CompletionResult(name, name, CompletionResultType.ParameterValue, name);
				}
			}

			return count > 0 ? array.AsSpan(0, count).ToArray() : [];
		}
		finally
		{
			ArrayPool<CompletionResult>.Shared.Return(array, clearArray: true);
		}
	}

	/// <summary>
	/// Gets the number that Sonarr gives the event type with the specified name.
	/// </summary>
	/// <remarks>
	/// The name has to be a whole event type name, ignoring case. Wildcards aren't matched.
	/// </remarks>
	/// <param name="type">The event type's name, or <see langword="null"/>.</param>
	/// <returns>
	/// The event type's number, or -1 when <paramref name="type"/> is <see langword="null"/>, empty, white space, or no
	/// event type's name.
	/// </returns>
	internal static int GetNumberFromEventType(string? type)
	{
		return !string.IsNullOrWhiteSpace(type) && _namesToInts.TryGetValue(type, out int value)
			? value
			: -1;
	}

	/// <summary>
	/// Determines whether an event type name starts with the specified word, which can hold wildcards.
	/// </summary>
	/// <remarks>
	/// The word is trimmed of white space and matched with a <c>*</c> after it, ignoring case, so a word that is only
	/// white space matches every name. The pattern is built on the stack, one character longer than the trimmed word.
	/// </remarks>
	/// <param name="name">The event type name.</param>
	/// <param name="word">The argument that is completed.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="name"/> matches <paramref name="word"/> followed by <c>*</c>;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool NameIsMatch(string name, ReadOnlySpan<char> word)
	{
		word = word.Trim();
		Span<char> chars = stackalloc char[word.Length + 1];
		word.CopyTo(chars);
		chars[word.Length] = '*';
		PatternMatcher matcher = new(chars);
		return matcher.IsMatch(name);
	}
	/// <summary>
	/// Creates the map from event type names to numbers, and the sorted names.
	/// </summary>
	/// <param name="names">When the method returns, the event type names, in ordinal order.</param>
	/// <returns>The map from each event type name to its number, whose keys ignore case.</returns>
	private static ImmutableDictionary<string, int> GetTypeDictionary(out string[] names)
	{
		var builder = ImmutableDictionary.CreateBuilder<string, int>(StringComparer.OrdinalIgnoreCase);
		names = Enum.GetNames<EpisodeHistoryEventType>();
		Array.Sort(names, StringComparer.Ordinal);

		foreach (string name in names)
		{
			builder.Add(name, (int)Enum.Parse<EpisodeHistoryEventType>(name, ignoreCase: false));
		}

		return builder.ToImmutable();
	}
	/// <summary>
	/// Represents the kinds of event that Sonarr records in an episode's history, with the numbers that Sonarr gives
	/// them.
	/// </summary>
	private enum EpisodeHistoryEventType
	{
		/// <summary>An event of no known kind.</summary>
		Unknown = 0,
		/// <summary>A release of the episode was grabbed and sent to a download client.</summary>
		Grabbed = 1,
		/// <summary>An episode file was imported from the series' folder.</summary>
		SeriesFolderImported = 2,
		/// <summary>An episode file was imported from a download client's folder.</summary>
		DownloadFolderImported = 3,
		/// <summary>A download of the episode failed.</summary>
		DownloadFailed = 4,
		/// <summary>The episode's file was deleted.</summary>
		EpisodeFileDeleted = 5,
		/// <summary>The episode's file was renamed.</summary>
		EpisodeFileRenamed = 6,
		/// <summary>A download of the episode was ignored.</summary>
		DownloadIgnored = 7
	}
}
