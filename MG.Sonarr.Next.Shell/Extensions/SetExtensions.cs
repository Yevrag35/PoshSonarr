using MG.Sonarr.Next.Shell.Components;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class SetExtensions
    {
        public static bool AnyValueLike(this IReadOnlySet<Wildcard> set, string? value)
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
        public static bool AnyValueLike(this IReadOnlySet<WildcardString> set, string? value)
        {
            foreach (WildcardString ws in set)
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
