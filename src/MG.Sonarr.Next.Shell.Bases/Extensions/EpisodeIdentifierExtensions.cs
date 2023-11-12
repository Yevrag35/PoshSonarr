using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Shell.Exceptions;
using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class EpisodeIdentifierExtensions
    {
        public static bool AreAllValid<T>([NotNullWhen(true)] this T[]? array, [NotNullWhen(false)] out ErrorRecord? error, [CallerArgumentExpression(nameof(array))] string argumentName = "") where T : IEpisodeIdentifier
        {
            if (array is null || array.Length <= 0)
            {
                error = FromException(new ArgumentException("No identifiers were specified", argumentName.Replace("this.", string.Empty)), array);
                return false;
            }

            List<T>? list = null;
            foreach (T item in array)
            {
                if (!item.IsValid())
                {
                    list ??= new(1);
                    list.Add(item);
                }
            }

            if (list is not null && list.Count > 0)
            {
                error = FromException(InvalidEpisodeIdFormatException.FromList(list, null), list);
                return false;
            }
            else
            {
                error = null;
                return true;
            }
        }

        public static IEnumerable<EpisodeObject> FilterEpisodes<T>(this T[] array, IEnumerable<EpisodeObject> collection)
            where T : IEpisodeIdentifier
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentNullException.ThrowIfNull(collection);

            foreach (EpisodeObject episode in collection)
            {
                foreach (T identifier in array)
                {
                    if (episode.Matches(identifier))
                    {
                        yield return episode;
                        break;
                    }
                }
            }
        }
        private static ErrorRecord FromException<T>(Exception exception, T? argument)
        {
            return exception.ToRecord(ErrorCategory.InvalidArgument, argument);
        }
    }
}