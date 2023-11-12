using MG.Sonarr.Next.Shell.Components;
using System.Buffers;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Management.Automation.Language;

namespace MG.Sonarr.Next.Shell.Completers
{
    internal sealed class EventTypeCompleter : IArgumentCompleter
    {
        private delegate bool PatternPredicate(in PatternMatcher matcher, ReadOnlySpan<char> value);
        private static readonly ImmutableDictionary<string, int> _namesToInts;
        private static readonly string[] _names;
        static EventTypeCompleter()
        {
            _namesToInts = GetTypeDictionary(out _names);
        }

        public EventTypeCompleter() { }

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

        internal static int GetNumberFromEventType(string? type)
        {
            return !string.IsNullOrWhiteSpace(type) && _namesToInts.TryGetValue(type, out int value)
                ? value
                : -1;
        }

        private static bool NameIsMatch(string name, ReadOnlySpan<char> word)
        {
            word = word.Trim();
            Span<char> chars = stackalloc char[word.Length + 1];
            word.CopyTo(chars);
            chars[word.Length] = '*';
            PatternMatcher matcher = new(chars);
            return matcher.IsMatch(name);
        }
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
