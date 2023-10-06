using MG.Sonarr.Next.Services.Attributes;
using MG.Sonarr.Next.Shell.Components;

namespace MG.Sonarr.Next.Shell.Extensions
{
    internal static class SetExtensions
    {
        internal static bool AnyValueLike([ValidatedNotNull] this IReadOnlySet<Wildcard> set, string? value)
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
