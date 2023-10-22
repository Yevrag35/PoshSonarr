using MG.Sonarr.Next.Shell.Components;
using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation.Language;

namespace MG.Sonarr.Next.Shell.Completers
{
    internal sealed class EventTypeCompleter : IArgumentCompleter
    {
        private delegate bool PatternPredicate(PatternMatcher matcher, ReadOnlySpan<char> value);

        static readonly Lazy<IReadOnlyDictionary<string, int>> _namesToInts = new(GetTypeDictionary);

        public EventTypeCompleter() { }

        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            bool empty = string.IsNullOrEmpty(wordToComplete);
            foreach (string name in _namesToInts.Value.Keys)
            {
                if (empty || NameIsMatch(name, wordToComplete))
                {
                    yield return new CompletionResult(name);
                }
            }
        }

        /// <exception cref="ArgumentException"></exception>
        internal static int GetNumberFromEventType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return -1;
            }
            else if (_namesToInts.Value.TryGetValue(type, out int value))
            {
                return value;
            }
            else if (int.TryParse(type, Statics.DefaultProvider, out int intResult))
            {
                return intResult;
            }
            else
            {
                throw new ArgumentException($"{type} is not a valid event type. Provide either a valid string or a number");
            }
        }

        private static bool NameIsMatch(ReadOnlySpan<char> value, ReadOnlySpan<char> word)
        {
            word = word.Trim();
            Span<char> chars = stackalloc char[word.Length + 1];
            word.CopyTo(chars);
            chars[word.Length] = '*';
            PatternMatcher matcher = new(chars);
            return matcher.IsMatch(value);
        }
        private static bool ValueIsMatch(PatternMatcher matcher, ReadOnlySpan<char> value)
        {
            return matcher.IsMatch(value);
        }
        private static IReadOnlyDictionary<string, int> GetTypeDictionary()
        {
            EpisodeHistoryEventType[] values = Enum.GetValues<EpisodeHistoryEventType>();
            SortedDictionary<string, int> dict = new(StringComparer.InvariantCultureIgnoreCase);

            foreach (var val in values)
            {
                dict.Add(val.ToString(), (int)val);
            }

            return new ReadOnlyDictionary<string, int>(dict);
        }
        private enum EpisodeHistoryEventType
        {
            Unknown = 0,
            Grabbed = 1,
            SeriesFolderImported = 2,
            DownloadFolderImported = 3,
            DownloadFailed = 4,
            EpisodeFileDeleted = 5,
            EpisodeFileRenamed = 6,
            DownloadIgnored = 7
        }
    }
}
