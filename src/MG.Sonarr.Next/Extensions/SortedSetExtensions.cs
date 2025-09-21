using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;

namespace MG.Sonarr.Next.Extensions
{
    public static class SortedSetExtensions
    {
        public static void AddRange(this SortedSet<int> ids, ISeriesPipeable[] collection)
        {
            foreach (ISeriesPipeable piped in collection.AsSpan())
            {
                _ = ids.Add(piped.SeriesId);
            }
        }
        public static void AddRange(this SortedSet<int> ids, IHasId[] collection)
        {
            foreach (IHasId piped in collection.AsSpan())
            {
                _ = ids.Add(piped.Id);
            }
        }
       
        public static void AddRange(this SortedSet<int> ids, ILanguageProfilePipeable[] collection)
        {
            foreach (ILanguageProfilePipeable piped in collection.AsSpan())
            {
                _ = ids.Add(piped.LanguageProfileId);
            }
        }
        public static void AddRange(this SortedSet<int> ids, IReleasePipeableBySeries[] collection)
        {
            foreach (IReleasePipeableBySeries piped in collection.AsSpan())
            {
                _ = ids.Add(piped.SeriesId);
            }
        }
        public static void AddRange(this SortedSet<int> ids, IReleasePipeableByEpisode[] collection)
        {
            foreach (IReleasePipeableByEpisode piped in collection.AsSpan())
            {
                _ = ids.Add(piped.EpisodeId);
            }
        }
    }
}
