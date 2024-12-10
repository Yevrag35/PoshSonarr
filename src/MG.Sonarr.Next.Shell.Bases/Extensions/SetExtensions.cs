using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Strings;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class SetExtensions
    {
        public static bool AnyValueLike<TCol>([ValidatedNotNull] this TCol set, string? value)
            where TCol : class, IEnumerable<Wildcard>
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
