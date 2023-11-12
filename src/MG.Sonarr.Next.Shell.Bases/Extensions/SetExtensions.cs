using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Shell.Components;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class SetExtensions
    {
        public static bool AnyValueLike([ValidatedNotNull] this IReadOnlySet<Wildcard> set, string? value)
        {
            foreach (Wildcard ws in set)
            {
                if (ws.IsMatch(value))
                { 
                    return true;
                }
            }

            return false;
        }
    }
}
